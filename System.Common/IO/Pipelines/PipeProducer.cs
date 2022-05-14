using static System.Verify;

namespace System.IO.Pipelines;

public abstract class PipeProducer : IAsyncDisposable
{
    private const int Stopped = 0;
    private const int Starting = 1;
    private const int Started = 2;
    private const int Stopping = 3;
    private readonly Pipe pipe;
    private readonly PipeReader reader;
    private readonly PipeWriter writer;
    private int disposed;
    private CancellationTokenSource globalCts;
    private Task producer;
    private long stateGuard;

    protected PipeProducer(PipeOptions pipeOptions = null)
    {
        pipe = new(pipeOptions ?? new PipeOptions(useSynchronizationContext: false));
        (reader, writer) = pipe;
    }

    public PipeReader Reader => reader;

    public Task Completion
    {
        get
        {
            var currentProducer = Volatile.Read(ref producer);
            ThrowIfInvalidState(Interlocked.Read(ref stateGuard) != Started || currentProducer is null);
            return currentProducer;
        }
    }

    public void Start()
    {
        CheckDisposed();

        switch (Interlocked.CompareExchange(ref stateGuard, Starting, Stopped))
        {
            case Stopped:
                try
                {
                    var cts = new CancellationTokenSource();
                    globalCts = cts;
                    producer = StartProducerAsync(writer, cts.Token);
                    Interlocked.Exchange(ref stateGuard, Started);
                }
                catch
                {
                    Interlocked.Exchange(ref stateGuard, Stopped);
                    throw;
                }

                break;
            case Stopping:
                ThrowInvalidState();
                break;
        }
    }

    public async Task ResetAsync()
    {
        await writer.CompleteAsync().ConfigureAwait(false);
        await reader.CompleteAsync().ConfigureAwait(false);
        pipe.Reset();
    }

    public async ValueTask StopAsync()
    {
        try
        {
            long state;
            var sw = new SpinWait();
            do
            {
                var localWorker = Volatile.Read(ref producer);
                var localCts = Volatile.Read(ref globalCts);
                state = Interlocked.CompareExchange(ref stateGuard, Stopping, Started);
                switch (state)
                {
                    case Starting:
                        sw.SpinOnce();
                        break;
                    case Started:
                        // we are responsible for cancellation and cleanup
                        using (localCts)
                        {
                            localCts!.Cancel();
                            try
                            {
                                await localWorker!.ConfigureAwait(false);
                            }
                            finally
                            {
                                Interlocked.Exchange(ref stateGuard, Stopped);
                            }
                        }

                        break;
                    case Stopping:
                        // stopping in progress already, wait for currently active task in flight
                        await localWorker!.ConfigureAwait(false);
                        break;
                }
            } while (state is Starting);
        }
#pragma warning disable CA1031 // producer loop termination exception should not be rethrown here
        catch
#pragma warning restore CA1031
        {
            // by design
        }
    }

    protected void CheckDisposed() => ThrowIfObjectDisposed(disposed is 1, nameof(PipeProducer));

    private async Task StartProducerAsync(PipeWriter pipeWriter, CancellationToken token)
    {
        try
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();

                var buffer = pipeWriter.GetMemory();

                var received = await ReceiveAsync(buffer, token).ConfigureAwait(false);

                if (received == 0)
                {
                    break;
                }

                pipeWriter.Advance(received);

                var result = await pipeWriter.FlushAsync(token).ConfigureAwait(false);

                if (result.IsCompleted || result.IsCanceled)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        finally
        {
            await pipeWriter.CompleteAsync().ConfigureAwait(false);
        }
    }

    protected abstract ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);

    #region Implementation of IAsyncDisposable

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref disposed, 1, 0) != 0) return;

        GC.SuppressFinalize(this);

        using (globalCts)
        {
            await StopAsync().ConfigureAwait(false);
        }
    }

    #endregion
}