using System.Net;
using System.Net.WebSockets;

namespace OOs.Net.Connections;

public class WebSocketServerConnection(WebSocket socket, EndPoint localEndPoint, EndPoint remoteEndPoint) : WebSocketConnection<WebSocket>(socket)
{
    public sealed override EndPoint LocalEndPoint => localEndPoint;

    public sealed override EndPoint RemoteEndPoint => remoteEndPoint;

    public override string ToString() => $"{Id}-WS ({remoteEndPoint})";

    protected override Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}