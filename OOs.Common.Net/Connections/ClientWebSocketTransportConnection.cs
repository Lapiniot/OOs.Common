using System.IO.Pipelines;
using System.Net.Sockets;
using System.Net.WebSockets;

#nullable enable

namespace OOs.Net.Connections;

public sealed class ClientWebSocketTransportConnection : WebSocketTransportConnection
{
    private readonly ClientWebSocket webSocket;
    private readonly HttpMessageInvoker? messageInvoker;
    private readonly Uri remoteUri;

    public ClientWebSocketTransportConnection(ClientWebSocket webSocket,
        Uri remoteUri, HttpMessageInvoker? messageInvoker,
        PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) :
        base(webSocket, null, new UriEndPoint(remoteUri), inputPipeOptions, outputPipeOptions)
    {
        ArgumentNullException.ThrowIfNull(remoteUri);

        this.webSocket = webSocket;
        this.remoteUri = remoteUri;
        this.messageInvoker = messageInvoker;
    }

    protected override async ValueTask OnStartingAsync(CancellationToken cancellationToken)
    {
        try
        {
            await webSocket.ConnectAsync(remoteUri, messageInvoker, cancellationToken).ConfigureAwait(false);
            // Discard HTTP Response Headers in order to release some memory
            webSocket.HttpResponseHeaders = null;
            await base.OnStartingAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (WebSocketException wse) when (wse.InnerException is HttpRequestException
        {
            InnerException: SocketException { SocketErrorCode: SocketError.HostNotFound }
        })
        {
            ThrowHelper.ThrowHostNotFound(wse);
        }
        catch (WebSocketException wse)
        {
            ThrowHelper.ThrowServerUnavailable(wse);
        }
    }

    public override string ToString() => $"{Id}-WS ({RemoteEndPoint})";

    public static ClientWebSocketTransportConnection Create(Uri remoteUri,
        Action<ClientWebSocketOptions>? configureOptions, HttpMessageInvoker? messageInvoker,
        PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null)
    {
        var socket = new ClientWebSocket();

        try
        {
            configureOptions?.Invoke(socket.Options);
            return new(socket, remoteUri, messageInvoker, inputPipeOptions, outputPipeOptions);
        }
        catch
        {
            using (socket) throw;
        }
    }
}