using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Connections;

public sealed class WebSocketClientConnection : WebSocketConnection<ClientWebSocket>
{
    private readonly X509Certificate[] clientCertificates;
    private readonly TimeSpan? keepAliveInterval;
    private readonly string[] subProtocols;

    public WebSocketClientConnection(Uri remoteUri, string[] subProtocols, X509Certificate[] clientCertificates = null, TimeSpan? keepAliveInterval = null)
        : base(null)
    {
        ArgumentNullException.ThrowIfNull(remoteUri);
        Verify.ThrowIfNullOrEmpty((Array)subProtocols);

        RemoteUri = remoteUri;
        this.subProtocols = subProtocols;
        this.clientCertificates = clientCertificates;
        this.keepAliveInterval = keepAliveInterval;
    }

    public Uri RemoteUri { get; }

    public IEnumerable<string> SubProtocols => subProtocols;

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        try
        {
            var ws = Socket = new();

            foreach (var subProtocol in SubProtocols)
            {
                ws.Options.AddSubProtocol(subProtocol);
            }

            if (clientCertificates is not null)
            {
                ws.Options.ClientCertificates.AddRange(clientCertificates);
            }

            if (keepAliveInterval.HasValue)
            {
                ws.Options.KeepAliveInterval = keepAliveInterval.Value;
            }

            await ws.ConnectAsync(RemoteUri, cancellationToken).ConfigureAwait(false);
        }
        catch (WebSocketException wse) when (
            wse.InnerException is HttpRequestException { InnerException: SocketException { SocketErrorCode: SocketError.HostNotFound } })
        {
            ThrowHostNotFound(wse);
        }
        catch (WebSocketException wse)
        {
            ThrowServerUnavailable(wse);
        }
    }

    public override string ToString() => $"{Id}-WS ({RemoteUri})";
}