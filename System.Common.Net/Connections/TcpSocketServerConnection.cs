using System.Net.Sockets;

namespace System.Net.Connections;

public sealed class TcpSocketServerConnection : SocketConnection
{
    public TcpSocketServerConnection(Socket acceptedSocket) : base(acceptedSocket)
    { }

    protected override Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}