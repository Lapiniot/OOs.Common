using static System.Net.IPAddress;
using static System.Net.Sockets.AddressFamily;
using static System.Net.Sockets.SocketType;
using static System.Net.Sockets.SocketOptionName;
using static System.Net.Sockets.SocketOptionLevel;
using System.Runtime.InteropServices;
using System.Buffers.Binary;

namespace System.Net.Sockets
{
    public delegate Socket CreateSocketFactory(IPEndPoint remoteEndPoint);

    public static class SocketBuilderExtensions
    {
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

        public static Socket ConfigureMulticastSender(this Socket socket, int mcintIndex, int ttl = 1, bool allowLoopback = true)
        {
            if(socket is null) throw new ArgumentNullException(nameof(socket));

            switch(socket.AddressFamily)
            {
                case InterNetwork:
                    socket.SetSocketOption(IP, MulticastInterface, HostToNetworkOrder(mcintIndex));
                    break;
                case InterNetworkV6:
                    socket.SetSocketOption(IPv6, MulticastInterface, mcintIndex);
                    break;

                default: throw new NotSupportedException("Unsupported address family");
            }

            socket.SetSocketOption(IP, MulticastTimeToLive, ttl);
            socket.SetSocketOption(IP, MulticastLoopback, allowLoopback);

            return socket;
        }

        public static Socket JoinMulticastGroup(this Socket socket, IPEndPoint groupToJoin, IPAddress mcint = null)
        {
            if(socket is null) throw new ArgumentNullException(nameof(socket));
            if(groupToJoin is null) throw new ArgumentNullException(nameof(groupToJoin));
            if(groupToJoin.AddressFamily != socket.AddressFamily) throw new NotSupportedException("Group address family mismatch");
            if(mcint is not null && mcint.AddressFamily != socket.AddressFamily) throw new NotSupportedException("Multicast interface address family mismatch");

            socket.SetSocketOption(SocketOptionLevel.Socket, ReuseAddress, true);
            socket.SetSocketOption(SocketOptionLevel.Socket, ExclusiveAddressUse, false);

            switch(socket.AddressFamily)
            {
                case InterNetwork:
                    socket.SetSocketOption(IP, AddMembership, new MulticastOption(groupToJoin.Address,
                        mcint ?? new IPAddress((int)socket.GetSocketOption(IP, MulticastInterface))));
                    socket.Bind(new IPEndPoint(Any, groupToJoin.Port));

                    if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        //  IP_MULTICAST_ALL (since Linux 2.6.31)
                        //  This option can be used to modify the delivery policy of multicast messages to sockets 
                        //  bound to the wildcard INADDR_ANY address. The argument is a boolean integer (defaults to 1).
                        //  If set to 1, the socket will receive messages from all the groups that have been joined 
                        //  globally on the whole system. Otherwise, it will deliver messages only from the groups that
                        //  have been explicitly joined (for example via the IP_ADD_MEMBERSHIP option) on this particular socket
                        const int IP_MULTICAST_All = 49;
                        Span<int> value = stackalloc int[1];
                        value[0] = 0;
                        socket.SetRawSocketOption(0, IP_MULTICAST_All, MemoryMarshal.AsBytes(value));
                    }

                    break;

                case InterNetworkV6:
                    socket.SetSocketOption(IPv6, AddMembership, new IPv6MulticastOption(groupToJoin.Address,
                        mcint is not null
                            ? BinaryPrimitives.ReadInt64BigEndian(mcint.GetAddressBytes())
                            : (long)socket.GetSocketOption(IPv6, MulticastInterface)));
                    socket.Bind(new IPEndPoint(IPv6Any, groupToJoin.Port));
                    break;
            }

            return socket;
        }
    }
}