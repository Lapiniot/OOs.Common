using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace OOs.Net.Connections;

public sealed class TcpSslSocketClientConnection : TcpSslSocketConnection
{
    private readonly X509Certificate[] certificates;
    private readonly SslProtocols enabledSslProtocols;
    private readonly string hostNameOrAddress;
    private readonly string machineName;
    private readonly int port;

    public TcpSslSocketClientConnection(IPEndPoint remoteEndPoint, string machineName,
        SslProtocols enabledSslProtocols = SslProtocols.None, X509Certificate[] certificates = null) :
        base(remoteEndPoint, reuseSocket: true)
    {
        ArgumentException.ThrowIfNullOrEmpty(machineName);

        this.machineName = machineName;
        this.enabledSslProtocols = enabledSslProtocols;
        this.certificates = certificates;
    }

    public TcpSslSocketClientConnection(string hostNameOrAddress, int port, string machineName = null,
        SslProtocols enabledSslProtocols = SslProtocols.None, X509Certificate[] certificates = null) :
            base(reuseSocket: true)
    {
        ArgumentException.ThrowIfNullOrEmpty(hostNameOrAddress);

        this.hostNameOrAddress = hostNameOrAddress;
        this.port = port;
        this.machineName = machineName ?? hostNameOrAddress;
        this.enabledSslProtocols = enabledSslProtocols;
        this.certificates = certificates;
    }

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        if (SslStream is not null)
            await SslStream.DisposeAsync().ConfigureAwait(false);

        await ConnectAsClientAsync(RemoteEndPoint ?? await ResolveRemoteEndPointAsync(hostNameOrAddress, port, cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

        SslStream = CreateSslStream(Socket);

        try
        {
            var options = new SslClientAuthenticationOptions
            {
                TargetHost = machineName,
                EnabledSslProtocols = enabledSslProtocols
            };

            if (certificates is not null)
                options.ClientCertificates = [.. certificates];

            await SslStream.AuthenticateAsClientAsync(options, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await using (SslStream.ConfigureAwait(false))
            {
                SslStream = null;
                throw;
            }
        }
    }

    public override string ToString() => $"{Id}-TCP.SSL ({RemoteEndPoint?.ToString() ?? "Not connected"})";
}