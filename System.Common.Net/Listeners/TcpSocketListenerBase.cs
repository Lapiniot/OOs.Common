using System.Net.Connections;
using System.Net.Sockets;

namespace System.Net.Listeners;

public abstract class TcpSocketListenerBase : IAsyncEnumerable<INetworkConnection>
{
    private readonly int backlog;
    private readonly Action<Socket> configureListening;
    private readonly Action<Socket> configureAccepted;
    private readonly IPEndPoint endPoint;

    protected TcpSocketListenerBase(IPEndPoint endPoint, int backlog = 100,
        Action<Socket> configureListening = null,
        Action<Socket> configureAccepted = null)
    {
        ArgumentNullException.ThrowIfNull(endPoint);

        this.endPoint = endPoint;
        this.backlog = backlog;
        this.configureListening = configureListening;
        this.configureAccepted = configureAccepted;
    }

    protected IPEndPoint EndPoint => endPoint;

    #region Implementation of IAsyncEnumerable<INetworkConnection>

    public async IAsyncEnumerator<INetworkConnection> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        using var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        if(endPoint.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // Allow IPv4 clients for backward compatibility, if endPoint designates IPv6 address
            socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        }
        configureListening?.Invoke(socket);

        socket.Bind(endPoint);
        socket.Listen(backlog);

        while(!cancellationToken.IsCancellationRequested)
        {
            Socket acceptedSocket = null;
            INetworkConnection connection = null;

            try
            {
                acceptedSocket = await socket.AcceptAsync(cancellationToken).ConfigureAwait(false);
                configureAccepted?.Invoke(acceptedSocket);
                connection = CreateConnection(acceptedSocket);
            }
            catch(OperationCanceledException)
            {
                acceptedSocket?.Dispose();
                yield break;
            }
            catch
            {
                acceptedSocket?.Dispose();
                throw;
            }

            if(connection is not null)
            {
                yield return connection;
            }
        }
    }

    #endregion

    protected abstract INetworkConnection CreateConnection(Socket acceptedSocket);
}