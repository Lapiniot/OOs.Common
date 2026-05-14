using System.Buffers;
using System.IO.Pipelines;

namespace OOs.Net.Connections;

/// <summary>
/// Provides base abstract implementation block for a transport connection that adapts existing networking APIs 
/// with asynchronous SendAsync/ReceiveAsync semantic to the "stream-like" PipeReader and PipeWriter interfaces.
/// </summary>
/// <param name="inputPipeOptions">The options for the input pipe.</param>
/// <param name="outputPipeOptions">The options for the output pipe.</param>
public abstract partial class TransportConnectionPipeAdapter(PipeOptions? inputPipeOptions,
    PipeOptions? outputPipeOptions) : TransportConnection
{
    protected static readonly PipeOptions DefaultInputPipeOptions = new(
        readerScheduler: PipeScheduler.ThreadPool,
        writerScheduler: PipeScheduler.Inline,
        useSynchronizationContext: false);

    protected static readonly PipeOptions DefaultOutputPipeOptions = new(
        readerScheduler: PipeScheduler.Inline,
        writerScheduler: PipeScheduler.ThreadPool,
        useSynchronizationContext: false);

    private readonly Pipe inputPipe = new(inputPipeOptions ?? DefaultInputPipeOptions);
    private readonly Pipe outputPipe = new(outputPipeOptions ?? DefaultOutputPipeOptions);
    private State state;
    private Task connectionClosed = Task.CompletedTask;

    public sealed override PipeReader Input => inputPipe.Reader;

    public sealed override PipeWriter Output => outputPipe.Writer;

    public override Task ConnectionClosed => connectionClosed;

    public sealed override async ValueTask StartAsync(CancellationToken cancellationToken)
    {
        switch (Interlocked.CompareExchange(ref state, State.Starting, State.Stopped))
        {
            case State.Stopped: break;
            case State.Starting: InvalidOperationException.ThrowInvalidState(nameof(State.Starting)); return;
            case State.Started: return;
            case State.Stopping: InvalidOperationException.ThrowInvalidState(nameof(State.Stopping)); return;
            case State.Disposed: ObjectDisposedException.ThrowIf(true, this); return;
        }

        try
        {
            await OnStartingAsync(cancellationToken).ConfigureAwait(false);
            connectionClosed = RunAsync();
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

        var inputWriter = inputPipe.Writer;
        var outputReader = outputPipe.Reader;

        try
        {
            var receiver = RunReceiverAsync(inputWriter);
            var sender = RunSenderAsync(outputReader);

            Interlocked.CompareExchange(ref state, State.Started, State.Starting);

            var completed = await Task.WhenAny(receiver, sender).ConfigureAwait(false);

            Interlocked.CompareExchange(ref state, State.Stopping, State.Started);

            if (completed == sender)
            {
                // This is highly expected branch. 
                // We normally get here when trasnport should be terminated due to one of:
                // - caller signal OUTPUT part activity should be gracefully stopped via call to Output.Complete()
                // - undrlaying connection is terminated by our side
                // - any other underlaying connection erroneous state first detected on our side
                // In this case also try to terminate receiver side gracefully cancelling potential pending flushes 
                // and then abort network connection itself. 
                // Wait until receiver task also transits to the completed state, 
                // so we can be sure that all IO activity is over on the underlaying connection.
                // We don't care about possible exceptions from receiver task, because sender task 
                // will have the real termination root cause.

                inputWriter.CancelPendingFlush();
                Abort();

                await receiver.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }
            else
            {
                // We normally get here due to one of:
                // - other side shuts down underlaying connection
                // - some connection related error happens

                outputReader.CancelPendingRead();
                Abort();

                await sender.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }

            await completed.ConfigureAwait(false);
        }
        finally
        {
            await OnStoppingAsync().AsTask().ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
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
    protected abstract ValueTask<int> ReceiveAsync(Memory<byte> buffer);
    protected abstract ValueTask SendAsync(ref readonly ReadOnlySequence<byte> buffer);

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