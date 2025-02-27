using System.IO.Pipelines;
using System.Net;

#nullable enable

namespace OOs.Net.Connections;

/// <summary>
/// Represents abstraction for generic bidirectional "stream-like" connection whose data 
/// can be read or written via corresponding <see cref="PipeReader"/> pipe
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
    /// Get <see cref="Task"/> that can be awaited to detect all transfer completion on this connection.
    /// When this task transits to completed state, all IO is done and connection is in the disconnected state, 
    /// so read/write operations cannot be performed anymore.
    /// </summary>
    public abstract Task Completion { get; }

    /// <summary>
    /// Initializes underlaying connection and starts IO operations. After this call <see cref="Input"/> and 
    /// <see cref="Output"/> can be used to perform read/write from the pipe.
    /// </summary>
    public abstract void Start();

    /// <summary>
    /// Aborts the underlaying connection and initiates <see cref="Completion"/> transition to the completed state.
    /// </summary>
    public abstract void Abort();

    /// <summary>
    /// Disposes resources for underlaying connection.
    /// </summary>
    /// <returns><see cref="ValueTask"/> that completes when resources are disposed.</returns>
    public virtual ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        Abort();
        return ValueTask.CompletedTask;
    }
}