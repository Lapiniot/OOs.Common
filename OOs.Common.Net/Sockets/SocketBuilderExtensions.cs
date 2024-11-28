using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using static System.Net.IPAddress;
using static System.Net.Sockets.AddressFamily;
using static System.Net.Sockets.SocketType;
using static System.Net.Sockets.SocketOptionName;
using static System.Net.Sockets.SocketOptionLevel;

namespace OOs.Net.Sockets;

public delegate Socket CreateSocketFactory(IPEndPoint remoteEndPoint);

public static class SocketBuilderExtensions
{
    public static IPEndPoint GetIPv4MulticastGroup(int port) => new(0xfaffffef /* 239.255.255.250 */, port);

    public static IPEndPoint GetIPv6MulticastGroup(int port) =>
        new(new IPAddress([0xff, 0x02, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x0c]) /* ff02::c (IPv6 link-local) */, port);

    public static IPEndPoint GetIPv4SSDPGroup() => GetIPv4MulticastGroup(1900);

    public static IPEndPoint GetIPv6SSDPGroup() => GetIPv6MulticastGroup(1900);

    public static UnixDomainSocketEndPoint ResolveUnixDomainSocketPath(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        return new(path.AsSpan().StartsWith("/@")
            ? string.Create(path.Length - 1, path, (span, p) => p.AsSpan(2).CopyTo(span.Slice(1)))
            : path);
    }

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

    public static Socket ConfigureMulticastOptions(this Socket socket, IPAddress mcintAddress, int ttl = 1, bool allowLoopback = true)
    {
        ArgumentNullException.ThrowIfNull(socket);

        var addressFamily = socket.AddressFamily;
        if (addressFamily is not (InterNetwork or InterNetworkV6))
        {
            ThrowNotSupportedAddressFamily();
        }

        var optionLevel = addressFamily is InterNetwork ? IP : IPv6;

        if (mcintAddress is not null)
        {
            if (mcintAddress.AddressFamily != addressFamily)
            {
                ThrowInterfaceAddressFamilyMismatch();
            }

            if (addressFamily is InterNetwork)
            {
                socket.SetSocketOption(optionLevel, MulticastInterface, mcintAddress.GetAddressBytes());
            }
            else
            {
                socket.SetSocketOption(optionLevel, MulticastInterface, (int)mcintAddress.ScopeId);
            }
        }

        socket.SetSocketOption(optionLevel, MulticastTimeToLive, ttl);
        socket.SetSocketOption(optionLevel, MulticastLoopback, allowLoopback);

        return socket;
    }

    public static Socket Bind(this Socket socket, int port = 0)
    {
        ArgumentNullException.ThrowIfNull(socket);

        socket.Bind(socket.AddressFamily is InterNetworkV6 ? new(IPv6Any, 0) : new IPEndPoint(Any, port));

        return socket;
    }

    public static Socket JoinMulticastGroup(this Socket socket, IPEndPoint groupToJoin, IPAddress mcintAddress = null)
    {
        ArgumentNullException.ThrowIfNull(socket);
        ArgumentNullException.ThrowIfNull(groupToJoin);

        if (groupToJoin.AddressFamily != socket.AddressFamily)
            ThrowGroupAddressFamilyMismatch();

        if (mcintAddress is not null && mcintAddress.AddressFamily != socket.AddressFamily)
            ThrowInterfaceAddressFamilyMismatch();

        socket.SetSocketOption(SocketOptionLevel.Socket, ReuseAddress, true);

        switch (socket.AddressFamily)
        {
            case InterNetwork:
                socket.SetSocketOption(IP, AddMembership, new MulticastOption(groupToJoin.Address, mcintAddress ?? Any));
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
                    var value = 0;
                    socket.SetRawSocketOption(0, IP_MULTICAST_ALL, MemoryMarshal.AsBytes(new ReadOnlySpan<int>(ref value)));
                }

                break;

            case InterNetworkV6:
                socket.SetSocketOption(IPv6, AddMembership, new IPv6MulticastOption(groupToJoin.Address, mcintAddress?.ScopeId ?? 0));
                socket.Bind(new IPEndPoint(IPv6Any, groupToJoin.Port));
                break;

            default:
                ThrowNotSupportedAddressFamily();
                break;
        }

        return socket;
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