using System.Net.Sockets;

namespace System.Net.Connections;

public sealed class UnixDomainSocketServerConnection(Socket acceptedSocket) : SocketConnection(acceptedSocket)
{
    protected override Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public override string ToString() => $"{Id}-UD";
}