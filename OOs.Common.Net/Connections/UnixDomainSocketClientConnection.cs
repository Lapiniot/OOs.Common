using System.Net.Sockets;

namespace OOs.Net.Connections;

[Obsolete("Consider usage of OOs.Net.Connections.ClientUnixDomainSocketTransportConnection instead.")]
public sealed class UnixDomainSocketClientConnection(UnixDomainSocketEndPoint remoteEndPoint) :
    SocketConnection(remoteEndPoint, ProtocolType.IP, reuseSocket: true)
{
    protected override Task StartingAsync(CancellationToken cancellationToken) =>
        ConnectAsClientAsync(RemoteEndPoint, cancellationToken);

    public override string ToString() => $"{Id}-UD ({RemoteEndPoint?.ToString() ?? "Not connected"})";
}