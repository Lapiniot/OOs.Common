using System.Net;
using System.Net.Sockets;
using OOs.Net.Connections;

namespace OOs.Net.Listeners;

public sealed class TcpSocketListener(IPEndPoint endPoint, int backlog = 100,
    Action<Socket> configureListening = null, Action<Socket> configureAccepted = null) :
    TcpSocketListenerBase(endPoint, backlog, configureListening, configureAccepted)
{
    protected override TransportConnection CreateConnection(Socket acceptedSocket) =>
        new ServerTcpSocketTransportConnection(acceptedSocket);

    public override string ToString() => $"{nameof(TcpSocketListener)} (tcp://{EndPoint})";
}