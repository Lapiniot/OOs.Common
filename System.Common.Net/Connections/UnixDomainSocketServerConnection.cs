using System.Net.Sockets;

namespace System.Net.Connections;

public sealed class UnixDomainSocketServerConnection : SocketConnection
{
    public UnixDomainSocketServerConnection(Socket acceptedSocket) : base(acceptedSocket)
    { }

    protected override Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}