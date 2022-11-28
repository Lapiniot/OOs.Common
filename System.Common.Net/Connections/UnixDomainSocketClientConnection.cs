using System.Net.Sockets;

namespace System.Net.Connections;

public sealed class UnixDomainSocketClientConnection : SocketConnection
{
    public UnixDomainSocketClientConnection(UnixDomainSocketEndPoint remoteEndPoint) :
        base(remoteEndPoint, ProtocolType.IP)
    {
    }

    protected override Task StartingAsync(CancellationToken cancellationToken) =>
        ConnectAsClientAsync(RemoteEndPoint, cancellationToken);
}