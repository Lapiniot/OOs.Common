using System.Net.WebSockets;

namespace System.Net.Connections;

public class WebSocketServerConnection : WebSocketConnection<WebSocket>
{
    private readonly IPEndPoint remoteEndPoint;

    public WebSocketServerConnection(WebSocket acceptedSocket, IPEndPoint remoteEndPoint) : base(acceptedSocket)
    {
        this.remoteEndPoint = remoteEndPoint;
    }

    public IPEndPoint RemoteEndPoint => remoteEndPoint;

    public override string ToString()
    {
        return $"{Id}-{nameof(WebSocketServerConnection)}-{remoteEndPoint}";
    }

    protected override Task StartingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}