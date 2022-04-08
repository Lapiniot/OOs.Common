using System.Net.Sockets;

namespace System.Net.Connections;

public sealed class TcpServerSocketConnection : TcpSocketConnection
{
    public TcpServerSocketConnection(Socket acceptedSocket) : base(acceptedSocket)
    { }

    protected override Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}