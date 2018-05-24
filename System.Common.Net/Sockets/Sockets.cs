using static System.Net.Sockets.AddressFamily;
using static System.Net.Sockets.SocketType;

namespace System.Net.Sockets
{
    public delegate Socket CreateSocketFactory();

    public delegate Socket CreateConnectedSocketFactory(IPEndPoint remoteEndPoint);

    public static class Sockets
    {
        private class UdpSocket : Socket
        {
            public UdpSocket() : base(InterNetwork, Dgram, ProtocolType.Udp)
            {
            }
        }

        public static class Udp
        {
            public static Socket Broadcast()
            {
                var socket = new UdpSocket();
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                return socket;
            }

            public static Socket Connected(IPEndPoint endpoint)
            {
                var socket = new UdpSocket();
                socket.Connect(endpoint);
                return socket;
            }

            public static class Multicast
            {
                public static Socket Sender()
                {
                    return new UdpSocket();
                }

                public static Socket Listener(IPEndPoint group)
                {
                    var socket = new UdpSocket();

                    socket.Bind(new IPEndPoint(IPAddress.Any, group.Port));

                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(group.Address));
                    socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 64);

                    return socket;
                }
            }
        }
    }
}