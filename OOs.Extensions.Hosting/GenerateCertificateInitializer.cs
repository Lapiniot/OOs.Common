using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using static OOs.Security.Cryptography.CertificateHelpers;

namespace OOs.Extensions.Hosting;

public class CertificateGenerateInitializer(IHostEnvironment environment, IConfiguration configuration) : IServiceInitializer
{
    public Task InitializeAsync(CancellationToken cancellationToken) =>
        InitializeAsync(environment, configuration, cancellationToken);

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public static Task InitializeAsync(IHostEnvironment environment, IConfiguration configuration, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(configuration);

        var appName = environment.ApplicationName;

        if (configuration.GetValue<string>("GENERATE_SSL_CERTIFICATE") is not { Length: > 0 } genCertOption)
        {
            return Task.CompletedTask;
        }

        var certPath = genCertOption.ToUpperInvariant() is "1" or "TRUE"
            ? Path.Combine(environment.GetAppConfigPath(), $"{appName}.pfx")
            : genCertOption;

        if (Path.Exists(certPath))
        {
            return Task.CompletedTask;
        }

        if (Path.GetDirectoryName(certPath) is { Length: > 0 } certDir)
        {
            Directory.CreateDirectory(certDir);
        }

        var dnsNames = GetDnsNames(configuration).ToList();
        var commonName = dnsNames.First();
        var bytes = GenerateSelfSignedCertificate(commonName, $"{appName} on {commonName}", "development",
            DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(365),
            dnsNames, GetIPAddresses(configuration));

        return File.WriteAllBytesAsync(certPath, bytes, cancellationToken);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' " +
        "require dynamic access otherwise can break functionality when trimming application code",
        Justification = "<Pending>")]
    private static IEnumerable<IPAddress> GetIPAddresses(IConfiguration configuration)
    {
        if (configuration.GetValue<string>("SSL_CERTIFICATE_IP_ADDRESSES") is { Length: > 0 } addresses)
        {
            foreach (var address in addresses.Split([',', ';', ' '], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                yield return IPAddress.Parse(address);
            }
        }
        else
        {
            var externalAddresses = from iface in NetworkInterface.GetAllNetworkInterfaces()
                                    where iface is
                                    {
                                        OperationalStatus: OperationalStatus.Up,
                                        NetworkInterfaceType: not (NetworkInterfaceType.Unknown
                                            or NetworkInterfaceType.Tunnel or NetworkInterfaceType.Loopback)
                                    }
                                    let properties = iface.GetIPProperties()
                                    where properties.GatewayAddresses.Count > 0
                                    from addressInformation in properties.UnicastAddresses
                                    select addressInformation.Address;
            foreach (var address in externalAddresses)
            {
                yield return address;
            }
        }

        yield return IPAddress.Loopback;
        yield return IPAddress.IPv6Loopback;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' " +
        "require dynamic access otherwise can break functionality when trimming application code",
        Justification = "<Pending>")]
    private static IEnumerable<string> GetDnsNames(IConfiguration configuration)
    {
        if (configuration.GetValue<string>("SSL_CERTIFICATE_HOSTS") is { Length: > 0 } dnsNames)
        {
            foreach (var dnsName in dnsNames.Split([',', ';', ' '], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                yield return dnsName;
            }
        }
        else
        {
            yield return Dns.GetHostName();
        }

        yield return "localhost";
    }
}