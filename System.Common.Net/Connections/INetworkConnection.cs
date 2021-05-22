using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Connections
{
    /// <summary>
    /// Represents abstraction for duplex network connection with asynchronouse Send/Receive
    /// support and connection state management 
    /// </summary>
    public interface INetworkConnection : IConnectedObject, IAsyncDisposable
    {
        /// <summary>
        /// Sends data to other party
        /// </summary>
        /// <param name="buffer">Memory buffer containing data to send</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>ValueTask that can be awaited</returns>
        ValueTask SendAsync(Memory<byte> buffer, CancellationToken cancellationToken);
        /// <summary>
        /// Receives data sent by other party
        /// </summary>
        /// <param name="buffer">Memory buffer containing data sent by other party</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of bytes succesfully received from other party</returns>
        ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);
        /// <summary>
        /// Correlation ID of current connection instance (used for debug and tracing purpose primarily). 
        /// </summary>
        /// <value>correlation ID value</value>
        public string Id { get; }
    }
}