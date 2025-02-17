using System.Net;
using System.Net.Sockets;
using OOs.Net.Connections;

namespace OOs.Net.Listeners;

public abstract class SocketListener : IAsyncEnumerable<TransportConnection>
{
    private readonly int backlog;
    private readonly Action<Socket> configureAccepted;
    private readonly Action<Socket> configureListening;
    private readonly EndPoint endPoint;

    protected SocketListener(EndPoint endPoint, int backlog = 100,
        Action<Socket> configureListening = null,
        Action<Socket> configureAccepted = null)
    {
        ArgumentNullException.ThrowIfNull(endPoint);

        this.endPoint = endPoint;
        this.backlog = backlog;
        this.configureListening = configureListening;
        this.configureAccepted = configureAccepted;
    }

    protected EndPoint EndPoint => endPoint;

    protected abstract Socket CreateSocket();
    protected abstract NetworkConnection CreateConnection(Socket acceptedSocket);

    #region Implementation of IAsyncEnumerable<INetworkConnection>

    public async IAsyncEnumerator<TransportConnection> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        using var socket = CreateSocket();

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
                yield return new NetworkConnectionAdapter(connection);
        }
    }

    #endregion
}