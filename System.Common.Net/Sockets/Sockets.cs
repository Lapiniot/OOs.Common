﻿using System.Linq;
using System.Net.NetworkInformation;
using static System.Net.IPAddress;
using static System.Net.NetworkInformation.NetworkInterface;
using static System.Net.NetworkInformation.OperationalStatus;
using static System.Net.Sockets.AddressFamily;
using static System.Net.Sockets.SocketType;
using static System.Net.Sockets.SocketOptionName;
using static System.Net.Sockets.SocketOptionLevel;

namespace System.Net.Sockets
{
    public delegate Socket CreateSocketFactory();

    public delegate Socket CreateConnectedSocketFactory(IPEndPoint remoteEndPoint);

    public static class SocketFactory
    {
        public static IPEndPoint GetIPv4MulticastGroup(int port)
        {
            return new IPEndPoint(new IPAddress(0xfaffffef /* 239.255.255.250 */), port);
        }

        public static IPEndPoint GetIPv4SsdpGroup()
        {
            return new IPEndPoint(new IPAddress(0xfaffffef /* 239.255.255.250 */), 1900);
        }

        public static Socket CreateUdpBroadcast(AddressFamily addressFamily)
        {
            return new Socket(addressFamily, Dgram, ProtocolType.Udp) { EnableBroadcast = true };
        }

        public static Socket CreateIPv4UdpBroadcast()
        {
            return CreateUdpBroadcast(InterNetwork);
        }

        public static Socket CreateIPv6UdpBroadcast()
        {
            return CreateUdpBroadcast(InterNetworkV6);
        }

        public static Socket CreateUdpConnected(IPEndPoint endpoint)
        {
            if(endpoint is null) throw new ArgumentNullException(nameof(endpoint));

            var socket = new Socket(endpoint.AddressFamily, Dgram, ProtocolType.Udp);
            socket.Connect(endpoint);

            return socket;
        }

        public static Socket CreateIPv4UdpMulticastSender()
        {
            var socket = new Socket(InterNetwork, Dgram, ProtocolType.Udp);

            var ipv4Properties = FindBestMulticastInterface().GetIPv4Properties() ??
                                 throw new InvalidOperationException("Cannot get interface IPv4 configuration data.");

            socket.SetSocketOption(IP, MulticastInterface, HostToNetworkOrder(ipv4Properties.Index));
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

            return socket;
        }

        public static Socket CreateIPv6UdpMulticastSender()
        {
            var socket = new Socket(InterNetworkV6, Dgram, ProtocolType.Udp);

            var ipv6Properties = FindBestMulticastInterface().GetIPv6Properties() ??
                                 throw new InvalidOperationException("Cannot get interface IPv6 configuration data.");

            socket.SetSocketOption(IPv6, MulticastInterface, ipv6Properties.Index);
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