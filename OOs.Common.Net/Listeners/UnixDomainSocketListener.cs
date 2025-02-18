using System.Net;
using System.Net.Sockets;
using OOs.Net.Connections;

namespace OOs.Net.Listeners;

public sealed class UnixDomainSocketListener(UnixDomainSocketEndPoint endPoint, int backlog = 100,
    Action<Socket> configureListening = null, Action<Socket> configureAccepted = null) :
    SocketListener(endPoint, backlog, configureListening, configureAccepted)
{
    protected override Socket CreateSocket()
    {
        var path = EndPoint.ToString();
        if (File.Exists(path)) File.Delete(path);

        return new(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
    }

    protected override TransportConnection CreateConnection(Socket acceptedSocket) =>
        new ServerUnixDomainSocketTransportConnection(acceptedSocket);

    public override string ToString() => $"{nameof(UnixDomainSocketListener)} (unix://{EndPoint})";
}