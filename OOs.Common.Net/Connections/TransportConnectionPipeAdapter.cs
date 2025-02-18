using System.IO.Pipelines;
using static System.Threading.Tasks.ConfigureAwaitOptions;

#nullable enable

namespace OOs.Net.Connections;

public abstract class TransportConnectionPipeAdapter(PipeOptions? inputPipeOptions, PipeOptions? outputPipeOptions) : TransportConnection
{
    private static readonly PipeOptions DefaultOptions = new(useSynchronizationContext: false);
    private const int Stopped = 0;
    private const int Starting = 1;
    private const int Started = 2;
    private const int Stopping = 3;
    private readonly Pipe inputPipe = new(inputPipeOptions ?? DefaultOptions);
    private readonly Pipe outputPipe = new(outputPipeOptions ?? DefaultOptions);
    private volatile CancellationTokenSource? globalCts;
    private volatile Task? inputWorker;
    private volatile Task? outputWorker;
    private int state;
    private int disposed;

    public sealed override PipeReader Input => inputPipe.Reader;

    public sealed override PipeWriter Output => outputPipe.Writer;

    public sealed override async Task CompleteOutputAsync()
    {
        await Output.CompleteAsync().ConfigureAwait(false);
        if (outputWorker is { } worker)
            await worker.ConfigureAwait(false);
    }

    public sealed override async ValueTask StartAsync(CancellationToken cancellationToken)
    {
        CheckDisposed();

        switch (Interlocked.CompareExchange(ref state, Starting, Stopped))
        {
            case Stopped:
                var cts = new CancellationTokenSource();
                try
                {
                    globalCts = cts;
                    Reset();
                    await OnStartingAsync(cancellationToken).ConfigureAwait(false);
                    inputWorker = StartInputPartAsync(inputPipe.Writer, cts.Token);
                    outputWorker = StartOutputPartAsync(outputPipe.Reader, cts.Token);
                    Volatile.Write(ref state, Started);
                }
                catch
                {
                    using (cts)
                    {
                        Volatile.Write(ref state, Stopped);
                        await cts.CancelAsync().ConfigureAwait(false);
                    }

                    throw;
                }

                break;
            case Stopping:
                OOs.ThrowHelper.ThrowInvalidState("Stopping");
                break;
        }
    }

    private void Reset()
    {
        inputPipe.Reader.Complete();
        inputPipe.Writer.Complete();
        inputPipe.Reset();

        outputPipe.Reader.Complete();
        outputPipe.Writer.Complete();
        outputPipe.Reset();
    }

    public sealed override Task StopAsync()
    {
        while (true)
        {
            switch (Interlocked.CompareExchange(ref state, Stopping, Started))
            {
                case Starting:
                    // We are currently starting, the object might be in semi-initialized state, 
                    // so better wait until started and then perform stopping gracefully.
                    // Startup is very quick, so we can use simple spinning here to 
                    // avoid heavy and expensive locking using SemaphoreSlim e.g.
                    var spinWait = new SpinWait();
                    do
                    {
                        spinWait.SpinOnce(sleep1Threshold: -1);
                    } while (Volatile.Read(ref state) is Starting);

                    break;

                case Started:
                    // we are responsible for cancellation and cleanup
                    return CancelAndWaitCompleteAsync();

                case Stopping:
                    // stopping is already in progress
                    return Task.WhenAll(inputWorker!, outputWorker!);

                default:
                    return Task.CompletedTask;
            }
        }

        async Task CancelAndWaitCompleteAsync()
        {
            var captured = globalCts!;
            using (captured)
            {
                await captured.CancelAsync().ConfigureAwait(SuppressThrowing);
                await Task.WhenAll(inputWorker!, outputWorker!).ConfigureAwait(SuppressThrowing);
                await OnStoppingAsync().AsTask().ConfigureAwait(SuppressThrowing);
                Volatile.Write(ref state, Stopped);
            }
        }
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

    protected abstract ValueTask OnStartingAsync(CancellationToken cancellationToken);
    protected abstract ValueTask OnStoppingAsync();
    protected abstract ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);
    protected abstract ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);

    #region Implementation of IAsyncDisposable

    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) is not 0)
            return;

        GC.SuppressFinalize(this);

        using (globalCts)
        {
            await StopAsync().ConfigureAwait(false);
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }

    #endregion
}