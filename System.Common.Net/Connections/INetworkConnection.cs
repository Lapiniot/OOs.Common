namespace System.Net.Connections;

/// <summary>
/// Represents abstraction for duplex network connection with asynchronous Send/Receive
/// support and connection state management
/// </summary>
public interface INetworkConnection : IAsyncDisposable
{
    /// <summary>
    /// Correlation ID of the current connection (used for debugging and tracing purpose primarily).
    /// </summary>
    /// <value>correlation ID value</value>
    public string Id { get; }

    /// <summary>
    /// Gets the local endpoint
    /// </summary>
    public EndPoint LocalEndPoint { get; }

    /// <summary>
    /// Gets the remote endpoint
    /// </summary>
    public EndPoint RemoteEndPoint { get; }

    /// <summary>
    /// Sends data to the other party
    /// </summary>
    /// <param name="buffer">Memory buffer containing data to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ValueTask that can be awaited</returns>
    ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);

    /// <summary>
    /// Receives data sent by other party
    /// </summary>
    /// <param name="buffer">Memory buffer containing data sent by other party</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of bytes successfully received from other party</returns>
    ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);
}