using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OOs.Net;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using static OOs.Security.Cryptography.CertificateHelpers;

namespace OOs.Extensions.Hosting;

public class CertificateGenerateInitializer(IHostEnvironment environment, IConfiguration configuration) : IServiceInitializer
{
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var appName = environment.ApplicationName;
        var configPath = environment.GetAppConfigPath();
        var certPath = Path.Combine(configPath, $"{appName}.pfx");
        var genCert = configuration.GetValue<string>("GENERATE_SSL_CERTIFICATE") is { Length: > 0 } value && (
            int.TryParse(value, out var n) && n is > 0 ||
            bool.TryParse(value, out var b) && b is true);

        if (genCert && !File.Exists(certPath))
        {
            Directory.CreateDirectory(configPath);

            var commonName = configuration.GetValue<string>("SSL_CERTIFICATE_HOSTNAME") ?? Dns.GetHostName();
            using var cert = CreateSelfSignedCertificate(
                BuildSubjectNameExtension(commonName, organization: $"UPNP Dashboard on {commonName}", organizationalUnit: "development"),
                BuildSubjectAlternateNamesExtension(
                    [commonName, "localhost"],
                    [IPAddress.Loopback, IPAddress.IPv6Loopback, .. NetworkInterface.GetAllNetworkInterfaces().GetActiveExternalInterfaces().
                        SelectMany(iface => iface.GetIPProperties().UnicastAddresses.Select(uai => uai.Address))]),
                notBefore: DateTimeOffset.UtcNow.AddDays(-1),
                notAfter: DateTimeOffset.UtcNow.AddDays(365));
            var bytes = cert.Export(X509ContentType.Pfx, "");
            await File.WriteAllBytesAsync(certPath, bytes, cancellationToken).ConfigureAwait(false);
        }
    }
}