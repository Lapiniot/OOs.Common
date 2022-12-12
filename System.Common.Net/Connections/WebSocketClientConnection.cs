using System.Net.Sockets;
using System.Net.WebSockets;

namespace System.Net.Connections;

public sealed class WebSocketClientConnection : WebSocketConnection<ClientWebSocket>
{
    private readonly Action<ClientWebSocketOptions> configureOptions;

    public WebSocketClientConnection(Uri remoteUri, Action<ClientWebSocketOptions> configureOptions)
        : base(null)
    {
        ArgumentNullException.ThrowIfNull(remoteUri);

        RemoteUri = remoteUri;
        this.configureOptions = configureOptions;
    }

    public Uri RemoteUri { get; }

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        try
        {
            var ws = Socket = new();
            configureOptions?.Invoke(ws.Options);
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