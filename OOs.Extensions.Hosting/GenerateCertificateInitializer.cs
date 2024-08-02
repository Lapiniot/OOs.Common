using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OOs.Net;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;
using static OOs.Security.Cryptography.CertificateHelpers;

namespace OOs.Extensions.Hosting;

public class CertificateGenerateInitializer(IHostEnvironment environment, IConfiguration configuration) : IServiceInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken) =>
        await InitializeAsync(environment, configuration, cancellationToken).ConfigureAwait(false);

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public static async Task InitializeAsync(IHostEnvironment environment, IConfiguration configuration, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(configuration);

        var appName = environment.ApplicationName;

        if (configuration.GetValue<string>("GENERATE_SSL_CERTIFICATE") is { Length: > 0 } value)
        {
            var certPath = value.ToUpperInvariant() is "1" or "TRUE" ? Path.Combine(environment.GetAppConfigPath()!, $"{appName}.pfx") : value;

            if (Path.Exists(certPath))
            {
                return;
            }

            if (Path.GetDirectoryName(certPath) is { Length: > 0 } certDir)
            {
                Directory.CreateDirectory(certDir);
            }

            var commonName = configuration.GetValue<string>("SSL_CERTIFICATE_HOSTNAME") ?? Dns.GetHostName();
            var bytes = GenerateSelfSignedCertificate(commonName, $"{appName} on {commonName}", "development",
                DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(365),
                dnsNames: [],
                ipAddresses: NetworkInterface.GetAllNetworkInterfaces().GetActiveExternalInterfaces().
                    SelectMany(iface => iface.GetIPProperties().UnicastAddresses.Select(uai => uai.Address)));

            await File.WriteAllBytesAsync(certPath, bytes, cancellationToken).ConfigureAwait(false);
        }
    }
}