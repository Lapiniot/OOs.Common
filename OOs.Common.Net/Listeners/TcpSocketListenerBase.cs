using System.Net;
using System.Net.Sockets;

namespace OOs.Net.Listeners;

public abstract class TcpSocketListenerBase : SocketListener
{
    protected TcpSocketListenerBase(IPEndPoint endPoint, int backlog = 100,
        Action<Socket> configureListening = null,
        Action<Socket> configureAccepted = null) :
        base(endPoint, backlog, configureListening, configureAccepted)
    { }

    protected override Socket CreateSocket()
    {
        var addressFamily = EndPoint.AddressFamily;
        var socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);

        if (addressFamily == AddressFamily.InterNetworkV6)
            // Allow IPv4 clients for backward compatibility, if endPoint designates IPv6 address
            socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);

        return socket;
    }
}