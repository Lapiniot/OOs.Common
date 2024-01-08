using System.Net.Sockets;

namespace OOs.Net.Connections;

public sealed class TcpSocketServerConnection(Socket acceptedSocket) : SocketConnection(acceptedSocket)
{
    protected override Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}