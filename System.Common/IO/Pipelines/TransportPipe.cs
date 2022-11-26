using static System.Verify;

namespace System.IO.Pipelines;

public abstract class TransportPipe : IDuplexPipe, IAsyncDisposable
{
    private const int Stopped = 0;
    private const int Starting = 1;
    private const int Started = 2;
    private const int Stopping = 3;
    private readonly Pipe inputPipe;
    private readonly Pipe outputPipe;
    private int disposed;
    private CancellationTokenSource globalCts;
    private Task inputWorker;
    private Task outputWorker;
    private long stateGuard;

    protected TransportPipe(PipeOptions pipeOptions = null)
    {
        var options = pipeOptions ?? new PipeOptions(useSynchronizationContext: false);
        inputPipe = new(options);
        outputPipe = new(options);
    }

    public Task InputCompletion
    {
        get
        {
            var current = Volatile.Read(ref inputWorker);
            ThrowIfInvalidState(Interlocked.Read(ref stateGuard) != Started || current is null);
            return current;
        }
    }

    public Task OutputCompletion
    {
        get
        {
            var current = Volatile.Read(ref outputWorker);
            ThrowIfInvalidState(Interlocked.Read(ref stateGuard) != Started || current is null);
            return current;
        }
    }

    public PipeReader Input => inputPipe.Reader;

    public PipeWriter Output => outputPipe.Writer;

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
                    inputWorker = StartInputPartAsync(inputPipe.Writer, cts.Token);
                    outputWorker = StartOutputPartAsync(outputPipe.Reader, cts.Token);
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

    public void Reset()
    {
        inputPipe.Reader.Complete();
        inputPipe.Writer.Complete();
        inputPipe.Reset();

        outputPipe.Reader.Complete();
        outputPipe.Writer.Complete();
        outputPipe.Reset();
    }

    public async ValueTask StopAsync()
    {
        try
        {
            long state;
            var sw = new SpinWait();
            do
            {
                var localInputWorker = Volatile.Read(ref inputWorker);
                var localOutputWorker = Volatile.Read(ref outputWorker);
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
                                await Task.WhenAll(inputWorker, outputWorker).ConfigureAwait(false);
                            }
                            finally
                            {
                                Interlocked.Exchange(ref stateGuard, Stopped);
                            }
                        }

                        break;
                    case Stopping:
                        // stopping in progress already, wait for currently active task in flight
                        await Task.WhenAll(inputWorker, outputWorker).ConfigureAwait(false);
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

    protected void CheckDisposed() => ThrowIfObjectDisposed(disposed is 1, nameof(TransportPipe));

    private async Task StartInputPartAsync(PipeWriter writer, CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                var buffer = writer.GetMemory();

                var received = await ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);

                if (received == 0)
                {
                    break;
                }

                writer.Advance(received);

                var result = await writer.FlushAsync(cancellationToken).ConfigureAwait(false);

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
            await writer.CompleteAsync().ConfigureAwait(false);
        }
    }

    private async Task StartOutputPartAsync(PipeReader reader, CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                var buffer = result.Buffer;

                // TODO: Test hot path when sequence consists of single span for potential performance impact
                foreach (var chunk in buffer)
                {
                    await SendAsync(chunk, cancellationToken).ConfigureAwait(false);
                }

                reader.AdvanceTo(buffer.End, buffer.End);

                if (result.IsCanceled || result.IsCompleted)
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
            await reader.CompleteAsync().ConfigureAwait(false);
        }
    }

    protected abstract ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);
    protected abstract ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);

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