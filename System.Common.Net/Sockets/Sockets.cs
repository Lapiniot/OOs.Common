using System.Linq;
using System.Net.NetworkInformation;
using static System.Net.IPAddress;
using static System.Net.NetworkInformation.NetworkInterfaceType;
using static System.Net.NetworkInformation.OperationalStatus;
using static System.Net.Sockets.AddressFamily;
using static System.Net.Sockets.ProtocolType;
using static System.Net.Sockets.SocketType;

namespace System.Net.Sockets
{
    public delegate Socket CreateSocketFactory();

    public delegate Socket CreateConnectedSocketFactory(IPEndPoint remoteEndPoint);

    public static class SocketFactory
    {
        public static Socket CreateUdpBroadcast(AddressFamily addressFamily = InterNetwork)
        {
            return new Socket(addressFamily, Dgram, Udp) { EnableBroadcast = true };
        }

        public static Socket CreateUdpConnected(IPEndPoint endpoint)
        {
            if(endpoint is null) throw new ArgumentNullException(nameof(endpoint));

            var socket = new Socket(endpoint.AddressFamily, Dgram, Udp);
            socket.Connect(endpoint);

            return socket;
        }

        public static Socket CreateUdpIPv4MulticastSender()
        {
            var socket = new Socket(InterNetwork, Dgram, Udp);

            var ipv4Properties = FindBestMulticastInterface().GetIPv4Properties() ??
                throw new InvalidOperationException("Cannot get interface IPv4 configuration data.");

            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, HostToNetworkOrder(ipv4Properties.Index));
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 1);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, true);

            return socket;
        }

        public static Socket CreateUdpIPv6MulticastSender()
        {
            var socket = new Socket(InterNetworkV6, Dgram, Udp);

            var ipv6Properties = FindBestMulticastInterface().GetIPv6Properties() ??
                throw new InvalidOperationException("Cannot get interface IPv6 configuration data.");

            socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastInterface, ipv6Properties.Index);
            socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastTimeToLive, 1);

            return socket;
        }

        public static Socket CreateUdpMulticastListener(IPEndPoint group)
        {
            if(group is null) throw new ArgumentNullException(nameof(group));

            var socket = new Socket(group.AddressFamily, Dgram, Udp);

            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            if(group.AddressFamily == InterNetwork)
            {
                socket.Bind(new IPEndPoint(Any, group.Port));
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(group.Address));
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 64);
            }
            else
            {
                socket.Bind(new IPEndPoint(IPv6Any, group.Port));
                socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, new IPv6MulticastOption(group.Address));
                socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastTimeToLive, 64);
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
            var iface = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(IsActiveMulticastEthernet) ??
                            throw new InvalidOperationException("No valid network interface with multicast support found.");
            return iface.GetIPProperties() ??
                            throw new InvalidOperationException("Cannot get interface IP configuration properties.");
        }
    }
}