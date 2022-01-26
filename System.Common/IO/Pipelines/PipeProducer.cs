using System.Properties;

namespace System.IO.Pipelines;

public abstract class PipeProducer : IAsyncDisposable
{
    private const int Stopped = 0;
    private const int Starting = 1;
    private const int Started = 2;
    private const int Stopping = 3;
    private bool disposed;
    private CancellationTokenSource globalCts;
    private readonly PipeReader reader;
    private readonly PipeWriter writer;
    private readonly Pipe pipe;
    private Task producer;
    private long stateGuard;

    public PipeReader Reader => reader;

    public Task Completion
    {
        get
        {
            var currentProducer = Volatile.Read(ref producer);
            return Interlocked.Read(ref stateGuard) != Started || currentProducer is null
                ? throw new InvalidOperationException(Strings.InvalidStateNotStarted)
                : currentProducer;
        }
    }

    protected PipeProducer(PipeOptions pipeOptions = null)
    {
        pipe = new(pipeOptions ?? new PipeOptions(useSynchronizationContext: false));
        (reader, writer) = pipe;
    }

    #region Implementation of IAsyncDisposable

    public async ValueTask DisposeAsync()
    {
        if(Volatile.Read(ref disposed)) return;

        GC.SuppressFinalize(this);

        using(globalCts)
        {
            try
            {
                await StopAsync().ConfigureAwait(false);
            }
            finally
            {
                Volatile.Write(ref disposed, true);
            }
        }
    }

    #endregion

    public void Start()
    {
        CheckDisposed();

        switch(Interlocked.CompareExchange(ref stateGuard, Starting, Stopped))
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
                throw new InvalidOperationException("Cannot start in this state, currently in stopping transition.");
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
                switch(state)
                {
                    case Starting: sw.SpinOnce(); break;
                    case Started:
                        // we are responsible for cancellation and cleanup
                        using(localCts)
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
            }
            while(state is Starting);
        }
#pragma warning disable CA1031 // producer loop termination exception should not be rethrown here
        catch
#pragma warning restore CA1031
        {
            // by design
        }
    }

    protected void CheckDisposed()
    {
        if(disposed) throw new InvalidOperationException(Strings.ObjectInstanceDisposed);
    }

    private async Task StartProducerAsync(PipeWriter pipeWriter, CancellationToken token)
    {
        try
        {
            while(!token.IsCancellationRequested)
            {
                var buffer = pipeWriter.GetMemory();

                var rt = ReceiveAsync(buffer, token);
                var received = rt.IsCompletedSuccessfully ? rt.Result : await rt.ConfigureAwait(false);

                if(received == 0)
                {
                    break;
                }

                pipeWriter.Advance(received);

                var ft = pipeWriter.FlushAsync(token);
                var result = ft.IsCompletedSuccessfully ? ft.Result : await ft.ConfigureAwait(false);

                if(result.IsCompleted || result.IsCanceled)
                {
                    break;
                }
            }

        }
        catch(OperationCanceledException)
        {
            // Expected
        }
        finally
        {
            await pipeWriter.CompleteAsync().ConfigureAwait(false);
        }
    }

    protected abstract ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);
}