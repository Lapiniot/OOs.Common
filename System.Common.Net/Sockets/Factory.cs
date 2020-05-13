using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using static System.Net.IPAddress;
using static System.Net.NetworkInformation.NetworkInterface;
using static System.Net.NetworkInformation.OperationalStatus;
using static System.Net.Sockets.AddressFamily;
using static System.Net.Sockets.SocketType;
using static System.Net.Sockets.SocketOptionName;
using static System.Net.Sockets.SocketOptionLevel;
using static System.Runtime.InteropServices.RuntimeInformation;

namespace System.Net.Sockets
{

    public delegate Socket CreateSocketFactory(IPEndPoint remoteEndPoint);

    public static class Factory
    {
        [DllImport("libc", EntryPoint = "setsockopt")]
        private static extern int macos_setsockopt(IntPtr socket, int level, int optname, IntPtr optval, uint optlen);

        [DllImport("libc.so.6", EntryPoint = "setsockopt")]
        private static extern int linux_setsockopt(IntPtr socket, int level, int optname, IntPtr optval, uint optlen);

        private const int IP_MULTICAST_ALL = 49;

        public static IPEndPoint GetIPv4MulticastGroup(int port)
        {
            return new IPEndPoint(new IPAddress(0xfaffffef /* 239.255.255.250 */), port);
        }

        public static IPEndPoint GetIPv4SSDPGroup()
        {
            return GetIPv4MulticastGroup(1900);
        }

        public static Socket CreateUdpBroadcast(AddressFamily addressFamily)
        {
            return new Socket(addressFamily, Dgram, ProtocolType.Udp) {EnableBroadcast = true};
        }

        public static Socket CreateUdpConnected(IPEndPoint endpoint)
        {
            if(endpoint is null) throw new ArgumentNullException(nameof(endpoint));

            var socket = new Socket(endpoint.AddressFamily, Dgram, ProtocolType.Udp);
            socket.Connect(endpoint);

            return socket;
        }

        public static Socket CreateUdpMulticastSender(IPEndPoint groupToJoin)
        {
            return groupToJoin?.AddressFamily switch
            {
                InterNetwork => CreateIPv4UdpMulticastSender(groupToJoin),
                InterNetworkV6 => CreateIPv6UdpMulticastSender(groupToJoin),
                _ => throw new NotSupportedException("Unsupported address family")
            };
        }

        public static Socket CreateUdpMulticastSender(AddressFamily addressFamily)
        {
            return addressFamily switch
            {
                InterNetwork => CreateIPv4UdpMulticastSender(),
                InterNetworkV6 => CreateIPv6UdpMulticastSender(),
                _ => throw new NotSupportedException("Unsupported address family")
            };
        }

        public static Socket CreateIPv4UdpMulticastSender()
        {
            var socket = new Socket(InterNetwork, Dgram, ProtocolType.Udp);

            var properties = FindBestMulticastInterface().GetIPv4Properties()
                             ?? throw new InvalidOperationException("Cannot get interface IPv4 configuration data.");

            socket.SetSocketOption(IP, MulticastInterface, HostToNetworkOrder(properties.Index));
            socket.SetSocketOption(IP, MulticastTimeToLive, 1);
            socket.SetSocketOption(IP, MulticastLoopback, true);

            return socket;
        }

        public static Socket CreateIPv4UdpMulticastSender(IPEndPoint groupToJoin)
        {
            if(groupToJoin is null) throw new ArgumentNullException(nameof(groupToJoin));

            var socket = CreateIPv4UdpMulticastSender();

            socket.Bind(new IPEndPoint(Any, groupToJoin.Port));
            socket.SetSocketOption(IP, AddMembership, new MulticastOption(groupToJoin.Address));

            if(IsOSPlatform(OSPlatform.Linux))
            {
                IntPtr ptr = Marshal.AllocHGlobal(sizeof(int));
                Marshal.WriteInt64(ptr, 0, 0);

                try
                {
                    linux_setsockopt(socket.Handle, 0, IP_MULTICAST_ALL, ptr, sizeof(int));
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }

            return socket;
        }

        public static Socket CreateIPv6UdpMulticastSender()
        {
            var socket = new Socket(InterNetworkV6, Dgram, ProtocolType.Udp);

            var properties = FindBestMulticastInterface().GetIPv6Properties() ??
                             throw new InvalidOperationException("Cannot get interface IPv6 configuration data.");

            socket.SetSocketOption(IPv6, MulticastInterface, properties.Index);
            socket.SetSocketOption(IPv6, MulticastTimeToLive, 1);

            return socket;
        }

        public static Socket CreateIPv6UdpMulticastSender(IPEndPoint groupToJoin)
        {
            if(groupToJoin is null) throw new ArgumentNullException(nameof(groupToJoin));

            var socket = CreateIPv6UdpMulticastSender();

            socket.Bind(new IPEndPoint(IPv6Any, groupToJoin.Port));
            socket.SetSocketOption(IPv6, AddMembership, new IPv6MulticastOption(groupToJoin.Address));

            return socket;
        }

        public static Socket CreateUdpMulticastListener(IPEndPoint group)
        {
            if(group is null) throw new ArgumentNullException(nameof(group));

            var socket = new Socket(group.AddressFamily, Dgram, ProtocolType.Udp);

            socket.SetSocketOption(SocketOptionLevel.Socket, ReuseAddress, true);

            if(group.AddressFamily == InterNetwork)
            {
                socket.Bind(new IPEndPoint(Any, group.Port));
                socket.SetSocketOption(IP, AddMembership, new MulticastOption(group.Address));
                socket.SetSocketOption(IP, MulticastTimeToLive, 1);
            }
            else
            {
                socket.Bind(new IPEndPoint(IPv6Any, group.Port));
                socket.SetSocketOption(IPv6, AddMembership, new IPv6MulticastOption(group.Address));
                socket.SetSocketOption(IPv6, MulticastTimeToLive, 1);
            }

            return socket;
        }

        private static bool IsActiveMulticastEthernet(NetworkInterface networkInterface)
        {
            return networkInterface.GetIPProperties().GatewayAddresses.Count > 0 &&
                   networkInterface.SupportsMulticast &&
                   networkInterface.OperationalStatus == Up;
        }

        private static IPInterfaceProperties FindBestMulticastInterface()
        {
            var networkInterface = GetAllNetworkInterfaces().FirstOrDefault(IsActiveMulticastEthernet) ??
                                   throw new InvalidOperationException("No valid network interface with multicast support found.");
            return networkInterface.GetIPProperties() ??
                   throw new InvalidOperationException("Cannot get interface IP configuration properties.");
        }
    }
}