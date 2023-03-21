﻿using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using static System.Net.IPAddress;
using static System.Net.Sockets.AddressFamily;
using static System.Net.Sockets.SocketType;
using static System.Net.Sockets.SocketOptionName;
using static System.Net.Sockets.SocketOptionLevel;
using static System.Buffers.Binary.BinaryPrimitives;

namespace System.Net.Sockets;

public delegate Socket CreateSocketFactory(IPEndPoint remoteEndPoint);

public static class SocketBuilderExtensions
{
    public static IPEndPoint GetIPv4MulticastGroup(int port) => new(new IPAddress(0xfaffffef /* 239.255.255.250 */), port);

    public static IPEndPoint GetIPv4SSDPGroup() => GetIPv4MulticastGroup(1900);

    public static Socket CreateUdp(AddressFamily addressFamily = InterNetwork) => new(addressFamily, Dgram, ProtocolType.Udp);

    public static Socket CreateUdpBroadcast(AddressFamily addressFamily) =>
        new(addressFamily, Dgram, ProtocolType.Udp) { EnableBroadcast = true };

    public static Socket CreateUdpConnected(IPEndPoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var socket = new Socket(endpoint.AddressFamily, Dgram, ProtocolType.Udp);
        socket.Connect(endpoint);

        return socket;
    }

    public static Socket ConfigureMulticast(this Socket socket, int mcintIndex, int ttl = 1, bool allowLoopback = true)
    {
        ArgumentNullException.ThrowIfNull(socket);

        switch (socket.AddressFamily)
        {
            case InterNetwork:
                socket.SetSocketOption(IP, MulticastInterface, HostToNetworkOrder(mcintIndex));
                break;
            case InterNetworkV6:
                socket.SetSocketOption(IPv6, MulticastInterface, mcintIndex);
                break;
            default:
                ThrowNotSupportedAddressFamily();
                break;
        }

        socket.SetSocketOption(IP, MulticastTimeToLive, ttl);
        socket.SetSocketOption(IP, MulticastLoopback, allowLoopback);

        return socket;
    }

    public static Socket Bind(this Socket socket, int port = 0)
    {
        ArgumentNullException.ThrowIfNull(socket);

        socket.Bind(socket.AddressFamily is InterNetworkV6 ? new(IPv6Any, 0) : new IPEndPoint(Any, port));

        return socket;
    }

    public static Socket JoinMulticastGroup(this Socket socket, IPEndPoint groupToJoin, IPAddress mcint = null)
    {
        ArgumentNullException.ThrowIfNull(socket);
        ArgumentNullException.ThrowIfNull(groupToJoin);

        if (groupToJoin.AddressFamily != socket.AddressFamily)
            ThrowGroupAddressFamilyMismatch();

        if (mcint is not null && mcint.AddressFamily != socket.AddressFamily)
            ThrowInterfaceAddressFamilyMismatch();

        socket.SetSocketOption(SocketOptionLevel.Socket, ReuseAddress, true);

        switch (socket.AddressFamily)
        {
            case InterNetwork:
                socket.SetSocketOption(IP, AddMembership, CreateMulticastOptionIPv4(socket, groupToJoin.Address, mcint));
                socket.Bind(new IPEndPoint(Any, groupToJoin.Port));

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    //  IP_MULTICAST_ALL (since Linux 2.6.31)
                    //  This option can be used to modify the delivery policy of multicast messages to sockets 
                    //  bound to the wildcard INADDR_ANY address. The argument is a boolean integer (defaults to 1).
                    //  If set to 1, the socket will receive messages from all the groups that have been joined 
                    //  globally on the whole system. Otherwise, it will deliver messages only from the groups that
                    //  have been explicitly joined (for example via the IP_ADD_MEMBERSHIP option) on this particular socket
                    const int IP_MULTICAST_ALL = 49;
                    Span<int> value = stackalloc int[1];
                    value[0] = 0;
                    socket.SetRawSocketOption(0, IP_MULTICAST_ALL, MemoryMarshal.AsBytes(value));
                }

                break;

            case InterNetworkV6:
                socket.SetSocketOption(IPv6, AddMembership, new IPv6MulticastOption(groupToJoin.Address,
                    mcint is not null
                        ? ReadInt64BigEndian(mcint.GetAddressBytes())
                        : (long)socket.GetSocketOption(IPv6, MulticastInterface)!));
                socket.Bind(new IPEndPoint(IPv6Any, groupToJoin.Port));
                break;

            default:
                ThrowNotSupportedAddressFamily();
                break;
        }

        return socket;
    }

    private static MulticastOption CreateMulticastOptionIPv4(Socket socket, IPAddress groupToJoin, IPAddress mcint)
    {
        if (mcint is not null)
        {
            Span<byte> bytes = stackalloc byte[4];
            mcint.TryWriteBytes(bytes, out _);
            // Address from range 0.x.x.x must be interpreted as interface index
            return bytes[0] == 0
                ? new(groupToJoin, ReadInt32BigEndian(bytes))
                : new(groupToJoin, mcint);
        }

        var iface = (int)socket.GetSocketOption(IP, MulticastInterface)!;

        // If most significant byte (in network order) is zero in the
        // mcast interface, it represents interface index rather than IP address
        return (HostToNetworkOrder(iface) & 0xFF000000) == 0x00000000
            ? new(groupToJoin, HostToNetworkOrder(iface))
            : new(groupToJoin, new IPAddress(iface));
    }

    [DoesNotReturn]
    private static void ThrowNotSupportedAddressFamily() =>
        throw new NotSupportedException("Unsupported address family.");

    [DoesNotReturn]
    private static void ThrowInterfaceAddressFamilyMismatch() =>
        throw new InvalidOperationException("Multicast interface address family mismatch.");

    [DoesNotReturn]
    private static void ThrowGroupAddressFamilyMismatch() =>
        throw new InvalidOperationException("Group address family mismatch.");
}