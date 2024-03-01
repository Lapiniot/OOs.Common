using System.Net.Sockets;

namespace OOs.Net.Connections;

public sealed class TcpSocketServerConnection(Socket acceptedSocket) : SocketConnection(acceptedSocket, false)
{
    protected override Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}