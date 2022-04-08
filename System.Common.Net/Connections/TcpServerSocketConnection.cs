using System.Net.Sockets;

namespace System.Net.Connections;

public sealed class TcpServerSocketConnection : TcpSocketConnection
{
    public TcpServerSocketConnection(Socket acceptedSocket)
    {
        ArgumentNullException.ThrowIfNull(acceptedSocket);
        Socket = acceptedSocket;
        RemoteEndPoint = (IPEndPoint)Socket.RemoteEndPoint;
    }

    protected override Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}