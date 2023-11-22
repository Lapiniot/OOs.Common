using System.Runtime.CompilerServices;

namespace System.IO.Pipelines;

public abstract class TransportPipe : IDuplexPipe, IAsyncDisposable
{
    private static readonly PipeOptions DefaultOptions = new(useSynchronizationContext: false);
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

    protected TransportPipe(PipeOptions inputPipeOptions, PipeOptions outputPipeOptions)
    {
        inputPipe = new(inputPipeOptions ?? DefaultOptions);
        outputPipe = new(outputPipeOptions ?? DefaultOptions);
    }

    public Task InputCompletion => Read(ref inputWorker);

    public Task OutputCompletion => Read(ref outputWorker);

    public PipeReader Input => inputPipe.Reader;

    public PipeWriter Output => outputPipe.Writer;

    private T Read<T>(ref T location, [CallerMemberName] string callerName = null) where T : class
    {
        if (Interlocked.Read(ref stateGuard) != Started)
        {
            ThrowHelper.ThrowInvalidState(callerName);
        }

        return Volatile.Read(ref location);
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
                    inputWorker = StartInputPartAsync(inputPipe.Writer, cts.Token);
                    outputWorker = StartOutputPartAsync(outputPipe.Reader, cts.Token);
                    Volatile.Write(ref stateGuard, Started);
                }
                catch
                {
                    Volatile.Write(ref stateGuard, Stopped);
                    throw;
                }

                break;
            case Stopping:
                ThrowHelper.ThrowInvalidState();
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
                            await localCts!.CancelAsync().ConfigureAwait(false);
                            try
                            {
                                await Task.WhenAll(inputWorker, outputWorker).ConfigureAwait(false);
                            }
                            finally
                            {
                                Volatile.Write(ref stateGuard, Stopped);
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
#pragma warning disable CA1031 // Do not catch general exception types
        catch { /* by design */ }
#pragma warning restore CA1031 // Do not catch general exception types
    }

    public async Task CompleteOutputAsync()
    {
        await Output.CompleteAsync().ConfigureAwait(false);
        await OutputCompletion.ConfigureAwait(false);
    }

    protected void CheckDisposed() => ObjectDisposedException.ThrowIf(disposed is 1, this);

    private async Task StartInputPartAsync(PipeWriter writer, CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                var buffer = writer.GetMemory();

                var received = await ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);

                if (received == 0)
                    break;

                writer.Advance(received);

                var result = await writer.FlushAsync(cancellationToken).ConfigureAwait(false);

                if (result.IsCompleted || result.IsCanceled)
                    break;
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

                if (result.IsCanceled)
                    break;

                var buffer = result.Buffer;

                // TODO: Test hot path when sequence consists of single span for potential performance impact
                foreach (var chunk in buffer)
                {
                    await SendAsync(chunk, cancellationToken).ConfigureAwait(false);
                }

                reader.AdvanceTo(buffer.End, buffer.End);

                if (result.IsCompleted)
                    break;
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
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;

        GC.SuppressFinalize(this);

        using (globalCts)
        {
            await StopAsync().ConfigureAwait(false);
        }
    }

    #endregion
}