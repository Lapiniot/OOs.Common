using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;

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

    public override void Abort()
    {
        inputPipe.Writer.CancelPendingFlush();
        outputPipe.Reader.CancelPendingRead();
    }

    public override ValueTask CloseOutputAsync()
    {
        return Output.CompleteAsync();
    }

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
                // Mark Output as complete to prevent further writes to the terminated 
                // pipe connection, if it was not already complete explicitly by the 
                // time we reach this point.
                await Output.CompleteAsync().ConfigureAwait(false);

                // Initiate connection shutdown in the output direction, informing other party 
                // that we are completely done sending data. Ideally we should expect 
                // the same favour from other side, also terminating connection politely,
                // so our reading loop is acknowledged about closure and can be stopped.
                await ShutdownAsync(ShutdownDirection.Send).ConfigureAwait(false);

                // Wait for another side properly closing connection by also sending FIN packet,
                // so our receiver loop can stop gracefully by reading all final bits of 
                // data until zero bytes available.

                // Notice: we deliberately allow "half-open" connection and do not force 
                // closure of reading loop right away to ensure all data is read as a 
                // way of proper connection closure acknowledgement by the other side.
                // If another party doesn't properly respond to our termination signal,
                // we may endup waiting in this semi-open state endlessly.
                // Thus, application code should await on ConnectionClosed task mindfully,
                // with some reasonable timeout and then abort connection "hard way" (calling
                // Abort() e.g.) in case of other party doesn't respect our termination intent.
                await receiver.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }
            else
            {
                // Here we act more aggressively and shutdown all transfers both directions right away, 
                // as far as reader loop is already terminated by some reason and we cannot recognize 
                // connection closure acknowledgement (as reading 0 bytes) by other party anyway.

                // Notice: we do not complete Input here to allow readers to read any 
                // remaining data accumulated in the pipe buffer. Readers should better explicitly call 
                // Complete() on the input pipe when they are done reading to release any resources 
                // as soon as possible, otherwise Input will be completed automatically in the 
                // DisposeAsync() method at some later time.

                await ShutdownAsync(ShutdownDirection.Both).ConfigureAwait(false);
                outputReader.CancelPendingRead();
                await sender.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }
        }
        finally
        {
            await OnStoppingAsync().AsTask().ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            Interlocked.CompareExchange(ref state, State.Stopped, State.Stopping);
        }
    }

    /// <summary>
    /// Represents the direction in which to shut down the transport connection.
    /// </summary>
    protected enum ShutdownDirection
    {
        /// <summary>
        /// Shuts down the send direction of the transport connection.
        /// </summary>
        Send,
        /// <summary>
        /// Shuts down the receive direction of the transport connection.
        /// </summary>
        Receive,
        /// <summary>
        /// Shuts down both directions of the transport connection.
        /// </summary>
        Both
    }

    /// <summary>
    /// Initiates shutdown of the transport connection in the specified direction.
    /// </summary>
    /// <param name="direction">The direction in which to shutdown the connection.</param>
    /// <return>A <see cref="ValueTask"/> representing the asynchronous operation.</return>
    /// <remarks>
    /// Derived classes should implement this method to provide the actual 
    /// shutdown logic in the most friendly way possible for graceful termination.
    /// </remarks>
    protected abstract ValueTask ShutdownAsync(ShutdownDirection direction);

    [DoesNotReturn]
    protected static T ThrowInvalidShutdownDirection<T>(ShutdownDirection mode,
        [CallerArgumentExpression(nameof(mode))] string? paramName = null) =>
        throw new ArgumentException("Invalid value for shutdown direction mode", paramName);

    private void Reset()
    {
        inputPipe.Complete();
        inputPipe.Reset();

        outputPipe.Complete();
        outputPipe.Reset();
    }

    /// <summary>
    /// Called when the transport connection is starting.Implementations should provide initialization logic 
    /// (e.g., setting up internal state, establishing connections, etc.) here.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    protected abstract ValueTask OnStartingAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Called when the transport connection is stopping.
    /// Implementations should provide cleanup logic (e.g., closing connections, releasing resources, etc.) here.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    protected abstract ValueTask OnStoppingAsync();

    /// <summary>
    /// Called when data is received on the transport connection.
    /// Implementations should provide the logic for receiving data from underlaying network connection here.
    /// </summary>
    /// <param name="buffer">The buffer to receive data into.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation with the number of bytes received.</returns>
    protected abstract ValueTask<int> ReceiveAsync(Memory<byte> buffer);

    /// <summary>
    /// Called when data is to be sent on the transport connection.
    /// Implementations should provide the logic for sending data over the underlaying network connection here.
    /// </summary>
    /// <param name="buffer">The buffer containing the data to send.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    protected abstract ValueTask SendAsync(ref readonly ReadOnlySequence<byte> buffer);

    #region Implementation of IAsyncDisposable

    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref state, State.Disposed) is State.Disposed)
        {
            return;
        }

        GC.SuppressFinalize(this);

        inputPipe.Complete();
        outputPipe.Complete();

        await base.DisposeAsync().AsTask().ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        await OnStoppingAsync().AsTask().ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }

    #endregion
}