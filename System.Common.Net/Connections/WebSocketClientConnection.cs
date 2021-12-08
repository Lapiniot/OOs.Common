using System.Net.Connections.Exceptions;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using static System.Net.Properties.Strings;

namespace System.Net.Connections;

public sealed class WebSocketClientConnection : WebSocketConnection<ClientWebSocket>
{
    private readonly string[] subProtocols;
    private readonly X509Certificate2[] clientCertificates;
    private readonly TimeSpan? keepAliveInterval;

    public WebSocketClientConnection(Uri remoteUri, string[] subProtocols, X509Certificate2[] clientCertificates = null, TimeSpan? keepAliveInterval = null)
        : base(null)
    {
        ArgumentNullException.ThrowIfNull(remoteUri);
        ArgumentNullException.ThrowIfNull(subProtocols);
        if(subProtocols.Length == 0) throw new ArgumentException(NoWsSubProtocol);

        RemoteUri = remoteUri;
        this.subProtocols = subProtocols;
        this.clientCertificates = clientCertificates;
        this.keepAliveInterval = keepAliveInterval;
    }

    public Uri RemoteUri { get; }

    public IEnumerable<string> SubProtocols => subProtocols;

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        var socket = new ClientWebSocket();

        foreach(var subProtocol in SubProtocols)
        {
            socket.Options.AddSubProtocol(subProtocol);
        }

        if(clientCertificates is not null)
        {
            socket.Options.ClientCertificates.AddRange(clientCertificates);
        }

        if(keepAliveInterval.HasValue)
        {
            socket.Options.KeepAliveInterval = keepAliveInterval.Value;
        }

        try
        {
            await socket.ConnectAsync(RemoteUri, cancellationToken).ConfigureAwait(false);
            Socket = socket;
        }
        catch(WebSocketException wse) when(
            wse.InnerException is HttpRequestException { InnerException: SocketException { SocketErrorCode: SocketError.HostNotFound } })
        {
            throw new HostNotFoundException(wse);
        }
        catch(WebSocketException wse)
        {
            throw new ServerUnavailableException(wse);
        }
    }

    public override string ToString()
    {
        return $"{Id}-{nameof(WebSocketClientConnection)}";
    }
}