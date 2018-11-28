﻿using System.Linq;
using System.Net.NetworkInformation;
using static System.Net.IPAddress;
using static System.Net.NetworkInformation.NetworkInterfaceType;
using static System.Net.NetworkInformation.OperationalStatus;
using static System.Net.Sockets.AddressFamily;
using static System.Net.Sockets.SocketOptionLevel;
using static System.Net.Sockets.SocketOptionName;
using static System.Net.Sockets.SocketType;

namespace System.Net.Sockets
{
    public delegate Socket CreateSocketFactory();

    public delegate Socket CreateConnectedSocketFactory(IPEndPoint remoteEndPoint);

    public static class Sockets
    {
        public static class EndPoint
        {
            public static readonly IPEndPoint Any = new IPEndPoint(IPAddress.Any, 0);
        }

        private sealed class UdpSocket : Socket
        {
            public UdpSocket(AddressFamily addressFamily = InterNetwork) : base(addressFamily, Dgram, ProtocolType.Udp) {}
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
                    var udpSocket = new UdpSocket();

                    foreach(var i in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        if(!i.SupportsMulticast || i.OperationalStatus != Up ||
                           i.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                           i.NetworkInterfaceType == Ppp ||
                           i.NetworkInterfaceType == GenericModem ||
                           i.NetworkInterfaceType == Tunnel)
                        {
                            continue;
                        }

                        var interfaceProperties = i.GetIPProperties();

                        if(interfaceProperties == null || !interfaceProperties.MulticastAddresses.Any()) continue;

                        var properties = interfaceProperties.GetIPv4Properties();

                        if(properties == null) continue;

                        udpSocket.SetSocketOption(IP, MulticastInterface, HostToNetworkOrder(properties.Index));
                    }

                    udpSocket.SetSocketOption(IP, MulticastTimeToLive, 1);
                    udpSocket.SetSocketOption(IP, MulticastLoopback, 1);

                    return udpSocket;
                }

                public static Socket Listener(IPEndPoint group)
                {
                    var socket = new UdpSocket {ExclusiveAddressUse = false};

                    socket.SetSocketOption(SocketOptionLevel.Socket, ReuseAddress, true);

                    socket.Bind(new IPEndPoint(Any, group.Port));

                    socket.SetSocketOption(IP, AddMembership, new MulticastOption(group.Address));
                    socket.SetSocketOption(IP, MulticastTimeToLive, 64);

                    return socket;
                }
            }
        }
    }
}