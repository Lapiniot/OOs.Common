using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using static System.Net.NetworkInformation.NetworkInterface;
using static System.Net.NetworkInformation.OperationalStatus;

namespace System.Net
{
    public static class Interfaces
    {
        public static IPInterfaceProperties FindBestMulticastInterface()
        {
            var networkInterface = GetAllNetworkInterfaces().FirstOrDefault(IsActiveMulticastEthernet) ??
                                   throw new InvalidOperationException("No valid network interface with multicast support found.");
            return networkInterface.GetIPProperties() ??
                   throw new InvalidOperationException("Cannot get interface IP configuration properties.");
        }

        public static bool IsActiveMulticastEthernet(this NetworkInterface networkInterface)
        {
            if(networkInterface is null) throw new ArgumentNullException(nameof(networkInterface));

            return networkInterface.GetIPProperties().GatewayAddresses.Count > 0 &&
                   networkInterface.SupportsMulticast &&
                   networkInterface.OperationalStatus == Up;
        }

        public static IEnumerable<NetworkInterface> GetActiveExternalInterfaces(this IEnumerable<NetworkInterface> interfaces)
        {
            return interfaces.Where(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
                                          ni.OperationalStatus == OperationalStatus.Up &&
                                          ni.GetIPProperties().GatewayAddresses.Any());
        }

        public static IPAddress FindExternalIPv4Address(this IEnumerable<NetworkInterface> interfaces)
        {
            return interfaces.FirstOrDefault(i => i.Supports(NetworkInterfaceComponent.IPv4))?.GetIPProperties()
                .UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork)?.Address;
        }

        public static IPAddress FindExternalIPv6Address(this IEnumerable<NetworkInterface> interfaces)
        {
            return interfaces.FirstOrDefault(i => i.Supports(NetworkInterfaceComponent.IPv6))?.GetIPProperties()
                .UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6)?.Address;
        }
    }
}