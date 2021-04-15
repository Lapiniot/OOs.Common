using System.Runtime.InteropServices;
using static System.Net.IPAddress;
using static System.Net.Sockets.AddressFamily;
using static System.Net.Sockets.SocketType;
using static System.Net.Sockets.SocketOptionName;
using static System.Net.Sockets.SocketOptionLevel;
using static System.Runtime.InteropServices.RuntimeInformation;

namespace System.Net.Sockets
{
    public delegate Socket CreateSocketFactory(IPEndPoint remoteEndPoint);

    public static class SocketBuilderExtensions
    {
        private const int IpMulticastAll = 49;

        public static IPEndPoint GetIPv4MulticastGroup(int port)
        {
            return new(new IPAddress(0xfaffffef /* 239.255.255.250 */), port);
        }

        public static IPEndPoint GetIPv4SSDPGroup()
        {
            return GetIPv4MulticastGroup(1900);
        }

        public static Socket CreateUdp(AddressFamily addressFamily = AddressFamily.InterNetwork)
        {
            return new(addressFamily, Dgram, ProtocolType.Udp);
        }

        public static Socket CreateUdpBroadcast(AddressFamily addressFamily)
        {
            return new(addressFamily, Dgram, ProtocolType.Udp) { EnableBroadcast = true };
        }

        public static Socket CreateUdpConnected(IPEndPoint endpoint)
        {
            if(endpoint is null) throw new ArgumentNullException(nameof(endpoint));

            var socket = new Socket(endpoint.AddressFamily, Dgram, ProtocolType.Udp);
            socket.Connect(endpoint);

            return socket;
        }

        public static Socket ConfigureMulticastSender(this Socket socket)
        {
            if(socket is null) throw new ArgumentNullException(nameof(socket));

            switch(socket.AddressFamily)
            {
                case InterNetwork:
                    {
                        var properties = Interfaces.FindBestMulticastInterface().GetIPv4Properties()
                                                     ?? throw new InvalidOperationException("Cannot get interface IPv4 configuration data.");

                        socket.SetSocketOption(IP, MulticastInterface, HostToNetworkOrder(properties.Index));
                        socket.SetSocketOption(IP, MulticastTimeToLive, 1);
                        socket.SetSocketOption(IP, MulticastLoopback, true);
                        break;
                    }

                case InterNetworkV6:
                    {
                        var properties = Interfaces.FindBestMulticastInterface().GetIPv6Properties() ??
                                                     throw new InvalidOperationException("Cannot get interface IPv6 configuration data.");

                        socket.SetSocketOption(IPv6, MulticastInterface, properties.Index);
                        socket.SetSocketOption(IPv6, MulticastTimeToLive, 1);
                        socket.SetSocketOption(IPv6, MulticastLoopback, true);
                        break;
                    }

                default: throw new NotSupportedException("Unsupported address family");
            }

            return socket;
        }

        public static Socket JoinMulticastGroup(this Socket socket, IPEndPoint groupToJoin)
        {
            if(socket is null) throw new ArgumentNullException(nameof(socket));
            if(groupToJoin is null) throw new ArgumentNullException(nameof(groupToJoin));

            var addressFamily = groupToJoin.AddressFamily;
            if(addressFamily != InterNetwork && addressFamily != InterNetworkV6)
            {
                throw new NotSupportedException("Unsupported address family");
            }

            socket.SetSocketOption(SocketOptionLevel.Socket, ReuseAddress, true);
            socket.SetSocketOption(SocketOptionLevel.Socket, ExclusiveAddressUse, false);

            switch(addressFamily)
            {
                case InterNetwork:
                    {
                        socket.SetSocketOption(IP, AddMembership, new MulticastOption(groupToJoin.Address));
                        socket.Bind(new IPEndPoint(Any, groupToJoin.Port));

                        if(IsOSPlatform(OSPlatform.Linux))
                        {
                            Span<int> value = stackalloc int[1];
                            value[0] = 0;
                            socket.SetRawSocketOption(0, IpMulticastAll, MemoryMarshal.AsBytes(value));
                        }

                        break;
                    }

                case InterNetworkV6:
                    socket.SetSocketOption(IPv6, AddMembership, new IPv6MulticastOption(groupToJoin.Address));
                    socket.Bind(new IPEndPoint(IPv6Any, groupToJoin.Port));
                    break;
            }

            return socket;
        }
    }
}