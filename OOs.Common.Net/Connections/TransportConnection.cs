using System.IO.Pipelines;
using System.Net;

namespace OOs.Net.Connections;

/// <summary>
/// Represents abstraction for generic bidirectional "stream-like" connection whose data 
/// can be read or written via corresponding <see cref="PipeReader"/> and <see cref="PipeWriter"/>.
/// </summary>
public abstract class TransportConnection : IDuplexPipe, IAsyncDisposable
{
    /// <summary>
    /// Gets <see cref="PipeReader"/> instance that can be used to read data from.
    /// </summary>
    public abstract PipeReader Input { get; }

    /// <summary>
    /// Gets <see cref="PipeWriter"/> instance that can be used to write data to.
    /// </summary>
    public abstract PipeWriter Output { get; }

    /// <summary>
    /// Get unique connection Id that can be used for debugging or telemetry tracing.
    /// </summary>
    public abstract string Id { get; }

    /// <summary>
    /// Gets local endpoint for connection.
    /// </summary>
    public abstract EndPoint? LocalEndPoint { get; }

    /// <summary>
    /// Gets remote endpoint for connection.
    /// </summary>
    public abstract EndPoint? RemoteEndPoint { get; }

    /// <summary>
    /// Get <see cref="Task"/> that can be awaited to detect all transfers are completed on this connection.
    /// When this task transits to completed state, all IO is done and connection is closed, 
    /// so read/write operations cannot be performed anymore.
    /// </summary>
    public abstract Task ConnectionClosed { get; }

    /// <summary>
    /// Initializes underlaying connection and starts IO operations. After this call completes 
    /// sucessfully <see cref="Input"/> and <see cref="Output"/> can be used to perform read/write ops from the pipe.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to signal about completion.</param>
    /// <returns>The <see cref="ValueTask"/> to be awaited for async. operation result.</returns>
    public abstract ValueTask StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Aborts the underlaying connection and initiates <see cref="ConnectionClosed"/> transition to the completed state.
    /// </summary>
    public abstract void Abort();

    /// <summary>
    /// Completes the output pipe and signals other connection side that no more data will be written.
    /// Implementors should provide protocol specific logic to initiate graceful connection closure.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> that completes when the output is done.</returns>
    public abstract ValueTask CloseOutputAsync();

    /// <summary>
    /// Disposes resources for underlaying connection.
    /// </summary>
    /// <returns><see cref="ValueTask"/> that completes when resources are disposed.</returns>
    public virtual async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        // Say consumers that we are done with IO transfer and do not allow writing to the pipe anymore
        await Output.CompleteAsync().ConfigureAwait(false);
        // Also notify producer side that we are done with reading and have no intention to consume data anymore
        await Input.CompleteAsync().ConfigureAwait(false);

        // Abort underlaying network connection to unblock any pending network operations
        Abort();

        if (ConnectionClosed is { } connectionClosed)
        {
            await connectionClosed.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }

    public override string ToString() => Id;
}