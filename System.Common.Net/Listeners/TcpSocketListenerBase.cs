using System.Net.Connections;
using System.Net.Sockets;

namespace System.Net.Listeners;

public abstract class TcpSocketListenerBase : IAsyncEnumerable<NetworkConnection>
{
    private readonly int backlog;
    private readonly Action<Socket> configureAccepted;
    private readonly Action<Socket> configureListening;
    private readonly EndPoint endPoint;
    private readonly ProtocolType protocolType;

    protected TcpSocketListenerBase(EndPoint endPoint, ProtocolType protocolType, int backlog = 100,
        Action<Socket> configureListening = null,
        Action<Socket> configureAccepted = null)
    {
        ArgumentNullException.ThrowIfNull(endPoint);

        this.endPoint = endPoint;
        this.backlog = backlog;
        this.configureListening = configureListening;
        this.configureAccepted = configureAccepted;
        this.protocolType = protocolType;
    }

    protected EndPoint EndPoint => endPoint;

    protected abstract NetworkConnection CreateConnection(Socket acceptedSocket);

    #region Implementation of IAsyncEnumerable<INetworkConnection>

    public async IAsyncEnumerator<NetworkConnection> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        using var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, protocolType);

        if (endPoint.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // Allow IPv4 clients for backward compatibility, if endPoint designates IPv6 address
            socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        }

        configureListening?.Invoke(socket);

        socket.Bind(endPoint);
        socket.Listen(backlog);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Socket acceptedSocket = null;
            NetworkConnection connection;

            try
            {
                acceptedSocket = await socket.AcceptAsync(cancellationToken).ConfigureAwait(false);
                configureAccepted?.Invoke(acceptedSocket);
                connection = CreateConnection(acceptedSocket);
            }
            catch (OperationCanceledException)
            {
                acceptedSocket?.Dispose();
                yield break;
            }
            catch
            {
                acceptedSocket?.Dispose();
                throw;
            }

            if (connection is not null)
            {
                yield return connection;
            }
        }
    }

    #endregion
}