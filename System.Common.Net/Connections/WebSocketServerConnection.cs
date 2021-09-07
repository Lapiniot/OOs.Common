using System.Net.WebSockets;

namespace System.Net.Connections;

public class WebSocketServerConnection : WebSocketConnection<WebSocket>
{
    private readonly IPEndPoint remoteEndPoint;

    public WebSocketServerConnection(WebSocket acceptedWebSocket, IPEndPoint remoteEndPoint) : base(acceptedWebSocket)
    {
        this.remoteEndPoint = remoteEndPoint;
    }

    public override string ToString()
    {
        return $"{Id}-{nameof(WebSocketServerConnection)}-{remoteEndPoint}";
    }

    #region Overrides of WebSocketTransportBase

    public override Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    #endregion
}