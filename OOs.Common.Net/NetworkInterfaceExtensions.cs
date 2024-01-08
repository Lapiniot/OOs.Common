using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using static System.Net.NetworkInformation.NetworkInterface;
using static System.Net.Sockets.AddressFamily;

namespace OOs.Net;

public static class NetworkInterfaceExtensions
{
    public static NetworkInterface FindBestMulticastInterface() =>
        GetAllNetworkInterfaces().FirstOrDefault(static iface => iface.IsActiveMulticastEnabled()) ??
        ThrowNoMulticastInterface();

    public static int GetIndex(this NetworkInterface networkInterface, AddressFamily addressFamily)
    {
        ArgumentNullException.ThrowIfNull(networkInterface);

        return addressFamily switch
        {
            InterNetwork => networkInterface.GetIPProperties().GetIPv4Properties().Index,
            InterNetworkV6 => networkInterface.GetIPProperties().GetIPv6Properties().Index,
            _ => ThrowUnsupportedAddressFamily()
        };
    }

    public static IPAddress GetPrimaryAddress(this NetworkInterface networkInterface, AddressFamily addressFamily)
    {
        ArgumentNullException.ThrowIfNull(networkInterface);

        return networkInterface.GetIPProperties().UnicastAddresses.First(a => a.Address.AddressFamily == addressFamily).Address;
    }

    public static bool IsActiveMulticastEnabled(this NetworkInterface networkInterface)
    {
        ArgumentNullException.ThrowIfNull(networkInterface);

        return networkInterface.GetIPProperties().GatewayAddresses.Count > 0 &&
               networkInterface.SupportsMulticast &&
               networkInterface.OperationalStatus == OperationalStatus.Up;
    }

    public static NetworkInterface FindByAddress(string adapterAddress) =>
        IPAddress.TryParse(adapterAddress, out var address)
            ? GetAllNetworkInterfaces().FirstOrDefault(i => i.GetIPProperties().UnicastAddresses.Any(ua => ua.Address.Equals(address)))
            : null;

    public static NetworkInterface FindByName(string adapterName) =>
        GetAllNetworkInterfaces().FirstOrDefault(i => i.Name == adapterName);

    public static NetworkInterface FindById(string adapterId) =>
        GetAllNetworkInterfaces().FirstOrDefault(i => i.Id == adapterId);

    public static IEnumerable<NetworkInterface> GetActiveExternalInterfaces(this IEnumerable<NetworkInterface> interfaces) =>
        interfaces.Where(static ni =>
            ni.OperationalStatus == OperationalStatus.Up &&
            ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
            ni.NetworkInterfaceType != NetworkInterfaceType.Unknown &&
            ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
            ni.GetIPProperties().GatewayAddresses.Count > 0);

    public static IPAddress FindExternalIPv4Address(this IEnumerable<NetworkInterface> interfaces) =>
        interfaces.FirstOrDefault(static i => i.Supports(NetworkInterfaceComponent.IPv4))?.GetIPProperties()
            .UnicastAddresses.FirstOrDefault(static a => a.Address.AddressFamily == InterNetwork)?.Address;

    public static IPAddress FindExternalIPv6Address(this IEnumerable<NetworkInterface> interfaces) =>
        interfaces.FirstOrDefault(static i => i.Supports(NetworkInterfaceComponent.IPv6))?.GetIPProperties()
            .UnicastAddresses.FirstOrDefault(static a => a.Address.AddressFamily == InterNetworkV6)?.Address;

    [DoesNotReturn]
    private static NetworkInterface ThrowNoMulticastInterface() =>
        throw new InvalidOperationException("No valid network interface with multicast support found.");

    [DoesNotReturn]
    private static int ThrowUnsupportedAddressFamily() =>
        throw new ArgumentException("Address family is not supported.");
}