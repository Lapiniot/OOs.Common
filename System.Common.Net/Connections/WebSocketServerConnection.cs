using System.Net.WebSockets;

namespace System.Net.Connections;

public class WebSocketServerConnection : WebSocketConnection<WebSocket>
{
    private readonly EndPoint localEndPoint;
    private readonly EndPoint remoteEndPoint;

    public WebSocketServerConnection(WebSocket socket, EndPoint localEndPoint, EndPoint remoteEndPoint) : base(socket)
    {
        this.localEndPoint = localEndPoint;
        this.remoteEndPoint = remoteEndPoint;
    }

    public sealed override EndPoint LocalEndPoint => localEndPoint;

    public sealed override EndPoint RemoteEndPoint => remoteEndPoint;

    public override string ToString() => $"{Id}-WS ({remoteEndPoint})";

    protected override Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}