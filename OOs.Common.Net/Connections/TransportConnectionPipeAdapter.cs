using System.IO.Pipelines;
using static System.Threading.Tasks.ConfigureAwaitOptions;

#nullable enable

namespace OOs.Net.Connections;

public abstract partial class TransportConnectionPipeAdapter(
    PipeOptions? inputPipeOptions, PipeOptions? outputPipeOptions) :
    TransportConnection
{
    private static readonly PipeOptions DefaultOptions = new(useSynchronizationContext: false);
    private const int Stopped = 0;
    private const int Starting = 1;
    private const int Started = 2;
    private readonly Pipe inputPipe = new(inputPipeOptions ?? DefaultOptions);
    private readonly Pipe outputPipe = new(outputPipeOptions ?? DefaultOptions);
    private int state;
#if NET9_0_OR_GREATER
    private bool disposed;
#else
    private int disposed;
#endif
    private Task completion = Task.CompletedTask;

    public sealed override PipeReader Input => inputPipe.Reader;

    public sealed override PipeWriter Output => outputPipe.Writer;

    public override Task Completion => completion;

    public sealed override void Start()
    {
        CheckDisposed();

        if (Interlocked.CompareExchange(ref state, Starting, Stopped) is not Stopped)
        {
            return;
        }

        try
        {
            completion = RunAsync();
            Volatile.Write(ref state, Started);
        }
        catch
        {
            Volatile.Write(ref state, Stopped);
            throw;
        }
    }

    private async Task RunAsync()
    {
        Reset();

        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        await OnStartingAsync().ConfigureAwait(false);

        try
        {
            var inputWorker = StartReceiverAsync(inputPipe.Writer, cancellationToken);
            var outputWorker = StartSenderAsync(outputPipe.Reader, cancellationToken);

            if (await Task.WhenAny(inputWorker, outputWorker).ConfigureAwait(false) == outputWorker)
            {
                // This is highly expected branch. We normally get here 
                // when trasnport should be terminated due to one of:
                // - caller signal OUTPUT part activity should be gracefully stopped via call to Output.Complete()
                // - undrlaying connection is terminated by the client side
                // - any other underlaying connection erroneous state
                // In this case also terminate INPUT activity by cancelling 
                // reading partner task respectively via cancellationToken
                await cts.CancelAsync().ConfigureAwait(SuppressThrowing);
                // Wait until inputWorker task also transits to the completed state, 
                // so we can be sure that all IO activity is over on the underlaying connection.
                // Notice: we purposely swallow potential exception here because we better observe outputWorker task 
                // for potential exception upon completion as it holds real root cause of the transport termination
                await inputWorker.ConfigureAwait(SuppressThrowing);
                await outputWorker.ConfigureAwait(false);
            }
            else
            {
                // Just do the same, but in opposite order.
                // We normally get here due to one of:
                // - client side gracefully shuts down underlaying connection
                // - our side aborts connection via call to the Abort() method
                // - some connection related error happens
                await cts.CancelAsync().ConfigureAwait(SuppressThrowing);
                await outputWorker.ConfigureAwait(SuppressThrowing);
                await inputWorker.ConfigureAwait(false);
            }
        }
        finally
        {
            await OnStoppingAsync().AsTask().ConfigureAwait(SuppressThrowing);
            Volatile.Write(ref state, Stopped);
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

    protected void CheckDisposed() => ObjectDisposedException.ThrowIf(
#if NET9_0_OR_GREATER
        disposed,
#else
        disposed is 1,
#endif
        this);

    protected abstract ValueTask OnStartingAsync();
    protected abstract ValueTask OnStoppingAsync();
    protected abstract ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);
    protected abstract ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);

    #region Implementation of IAsyncDisposable

    public override ValueTask DisposeAsync()
    {
#if NET9_0_OR_GREATER
        if (Interlocked.Exchange(ref disposed, true))
#else
        if (Interlocked.Exchange(ref disposed, 1) is not 0)
#endif
            return ValueTask.CompletedTask;

        GC.SuppressFinalize(this);

        return base.DisposeAsync();
    }

    #endregion
}