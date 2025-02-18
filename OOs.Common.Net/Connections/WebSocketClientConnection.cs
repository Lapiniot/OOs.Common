using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace OOs.Net.Connections;

public sealed class WebSocketClientConnection : WebSocketConnection<ClientWebSocket>
{
    private readonly Action<ClientWebSocketOptions> configureOptions;
    private readonly HttpMessageInvoker invoker;
    private readonly EndPoint remoteEndPoint;

    public WebSocketClientConnection(Uri remoteUri, Action<ClientWebSocketOptions> configureOptions, HttpMessageInvoker invoker)
        : base(null)
    {
        ArgumentNullException.ThrowIfNull(remoteUri);

        RemoteUri = remoteUri;
        remoteEndPoint = new UriEndPoint(RemoteUri);
        this.configureOptions = configureOptions;
        this.invoker = invoker;
    }

    public Uri RemoteUri { get; }

    public override EndPoint LocalEndPoint => null;

    public override EndPoint RemoteEndPoint => remoteEndPoint;

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
            ThrowHelper.ThrowHostNotFound(wse);
        }
        catch (WebSocketException wse)
        {
            ThrowHelper.ThrowServerUnavailable(wse);
        }
    }

    public override string ToString() => $"{Id}-WS ({RemoteUri})";
}