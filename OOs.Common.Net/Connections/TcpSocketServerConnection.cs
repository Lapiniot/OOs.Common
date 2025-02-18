using System.Net.Sockets;

namespace OOs.Net.Connections;

[Obsolete("Consider usage of OOs.Net.Connections.ServerTcpSocketTransportConnection instead.")]
public sealed class TcpSocketServerConnection(Socket acceptedSocket) : SocketConnection(acceptedSocket, false)
{
    protected override Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}