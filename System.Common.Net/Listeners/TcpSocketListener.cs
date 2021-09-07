using System.Net.Connections;
using System.Net.Sockets;

namespace System.Net.Listeners;

public sealed class TcpSocketListener : TcpSocketListenerBase
{
    public TcpSocketListener(IPEndPoint endPoint, int backlog = 100,
        Action<Socket> configureListening = null,
        Action<Socket> configureAccepted = null) :
        base(endPoint, backlog, configureListening, configureAccepted)
    { }

    protected override INetworkConnection CreateConnection(Socket acceptedSocket)
    {
        return new TcpSocketServerConnection(acceptedSocket);
    }

    public override string ToString()
    {
        return $"{nameof(TcpSocketListener)} {{tcp://{EndPoint}}}";
    }
}