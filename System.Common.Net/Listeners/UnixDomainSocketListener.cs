using System.Net.Connections;
using System.Net.Sockets;

namespace System.Net.Listeners;

public sealed class UnixDomainSocketListener : SocketListener
{
    public UnixDomainSocketListener(UnixDomainSocketEndPoint endPoint, int backlog = 100,
        Action<Socket> configureListening = null,
        Action<Socket> configureAccepted = null) :
        base(endPoint, backlog, configureListening, configureAccepted)
    { }

    protected override Socket CreateSocket()
    {
        var path = EndPoint.ToString();
        if (File.Exists(path)) File.Delete(path);

        return new(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
    }

    protected override NetworkConnection CreateConnection(Socket acceptedSocket) =>
        new UnixDomainSocketServerConnection(acceptedSocket);

    public override string ToString() => $"{nameof(UnixDomainSocketListener)} (unix://{EndPoint})";
}