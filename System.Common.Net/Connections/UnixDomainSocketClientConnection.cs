using System.Net.Sockets;

namespace System.Net.Connections;

public sealed class UnixDomainSocketClientConnection(UnixDomainSocketEndPoint remoteEndPoint) : SocketConnection(remoteEndPoint, ProtocolType.IP)
{
    protected override Task StartingAsync(CancellationToken cancellationToken) =>
        ConnectAsClientAsync(RemoteEndPoint, cancellationToken);

    public override string ToString() => $"{Id}-UD ({RemoteEndPoint?.ToString() ?? "Not connected"})";
}