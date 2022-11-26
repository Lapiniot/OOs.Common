using System.Net.Connections;
using System.Net.Sockets;

namespace System.Net.Listeners;

public sealed class UnixDomainSocketListener : TcpSocketListenerBase
{
    public UnixDomainSocketListener(UnixDomainSocketEndPoint endPoint, int backlog = 100,
        Action<Socket> configureListening = null,
        Action<Socket> configureAccepted = null) :
        base(endPoint, ProtocolType.IP, backlog, configureListening, configureAccepted)
    { }

    protected override NetworkConnection CreateConnection(Socket acceptedSocket) => new TcpServerSocketConnection(acceptedSocket);

    public override string ToString() => $"{nameof(UnixDomainSocketListener)} (unix://{EndPoint})";
}