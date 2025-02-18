using System.Net.Sockets;

namespace OOs.Net.Connections;

[Obsolete("Consider usage of OOs.Net.Connections.ServerUnixDomainSocketTransportConnection instead.")]
public sealed class UnixDomainSocketServerConnection(Socket acceptedSocket) :
    SocketConnection(acceptedSocket, reuseSocket: false)
{
    protected override Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public override string ToString() => $"{Id}-UD";
}