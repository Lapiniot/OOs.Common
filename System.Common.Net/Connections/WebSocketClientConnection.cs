using System.Net.Sockets;
using System.Net.WebSockets;

namespace System.Net.Connections;

public sealed class WebSocketClientConnection : WebSocketConnection<ClientWebSocket>
{
    private readonly Action<ClientWebSocketOptions> configureOptions;
    private readonly HttpMessageInvoker invoker;

    public WebSocketClientConnection(Uri remoteUri, Action<ClientWebSocketOptions> configureOptions, HttpMessageInvoker invoker)
        : base(null)
    {
        ArgumentNullException.ThrowIfNull(remoteUri);

        RemoteUri = remoteUri;
        this.configureOptions = configureOptions;
        this.invoker = invoker;
    }

    public Uri RemoteUri { get; }

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        try
        {
            var socket = Socket = new();
            configureOptions?.Invoke(socket.Options);
            await socket.ConnectAsync(RemoteUri, invoker, cancellationToken).ConfigureAwait(false);
            // Discard HTTP Response Headers in order to release some memory
            socket.HttpResponseHeaders = null;
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