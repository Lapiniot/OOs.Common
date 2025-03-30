using System.IO.Pipelines;
using static System.Threading.Tasks.ConfigureAwaitOptions;

#nullable enable

namespace OOs.Net.Connections;

public abstract partial class TransportConnectionPipeAdapter(
    PipeOptions? inputPipeOptions, PipeOptions? outputPipeOptions) :
    TransportConnection
{
#if !NET9_0_OR_GREATER
    private static class State
    {
        public const int Stopped = 0;
        public const int Starting = 1;
        public const int Started = 2;
        public const int Stopping = 3;
        public const int Disposed = 4;
    }
#endif

    private static readonly PipeOptions DefaultOptions = new(useSynchronizationContext: false);
    private readonly Pipe inputPipe = new(inputPipeOptions ?? DefaultOptions);
    private readonly Pipe outputPipe = new(outputPipeOptions ?? DefaultOptions);
#if NET9_0_OR_GREATER
    private State state;
#else
    private int state;
#endif
    private Task completion = Task.CompletedTask;

    public sealed override PipeReader Input => inputPipe.Reader;

    public sealed override PipeWriter Output => outputPipe.Writer;

    public override Task Completion => completion;

    public sealed override async ValueTask StartAsync(CancellationToken cancellationToken)
    {
        switch (Interlocked.CompareExchange(ref state, State.Starting, State.Stopped))
        {
            case State.Stopped: break;
            case State.Starting: OOs.ThrowHelper.ThrowInvalidState(nameof(State.Starting)); return;
            case State.Started: return;
            case State.Stopping: OOs.ThrowHelper.ThrowInvalidState(nameof(State.Stopping)); return;
            case State.Disposed: ObjectDisposedException.ThrowIf(true, this); return;
        }

        try
        {
            await OnStartingAsync(cancellationToken).ConfigureAwait(false);
            completion = RunAsync();
        }
        catch
        {
            Interlocked.CompareExchange(ref state, State.Stopped, State.Starting);
            throw;
        }
    }

    private async Task RunAsync()
    {
        Reset();

        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        try
        {
            var receiver = StartReceiverAsync(inputPipe.Writer, cancellationToken);
            var sender = StartSenderAsync(outputPipe.Reader, cancellationToken);
            Interlocked.CompareExchange(ref state, State.Started, State.Starting);

            var completed = await Task.WhenAny(receiver, sender).ConfigureAwait(false);
            Interlocked.CompareExchange(ref state, State.Stopping, State.Started);
            await cts.CancelAsync().ConfigureAwait(SuppressThrowing);

            if (completed == sender)
            {
                // This is highly expected branch. We normally get here 
                // when trasnport should be terminated due to one of:
                // - caller signal OUTPUT part activity should be gracefully stopped via call to Output.Complete()
                // - undrlaying connection is terminated by the client side
                // - any other underlaying connection erroneous state
                // In this case also terminate INPUT activity by cancelling 
                // reading partner task respectively via cancellationToken
                // Wait until inputWorker task also transits to the completed state, 
                // so we can be sure that all IO activity is over on the underlaying connection.
                // Notice: we purposely swallow potential exception here because we better observe outputWorker task 
                // for potential exception upon completion as it holds real root cause of the transport termination
                await receiver.ConfigureAwait(SuppressThrowing);
                await sender.ConfigureAwait(false);
            }
            else
            {
                // Just do the same, but in opposite order.
                // We normally get here due to one of:
                // - client side gracefully shuts down underlaying connection
                // - our side aborts connection via call to the Abort() method
                // - some connection related error happens
                await sender.ConfigureAwait(SuppressThrowing);
                await receiver.ConfigureAwait(false);
            }
        }
        finally
        {
            await OnStoppingAsync().AsTask().ConfigureAwait(SuppressThrowing);
            Interlocked.CompareExchange(ref state, State.Stopped, State.Stopping);
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

    protected abstract ValueTask OnStartingAsync(CancellationToken cancellationToken);
    protected abstract ValueTask OnStoppingAsync();
    protected abstract ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);
    protected abstract ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);

    #region Implementation of IAsyncDisposable

    public override ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref state, State.Disposed) is State.Disposed)
        {
            return ValueTask.CompletedTask;
        }

        GC.SuppressFinalize(this);

        return base.DisposeAsync();
    }

    #endregion
}