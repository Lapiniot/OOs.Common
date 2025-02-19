using System.IO.Pipelines;
using System.Net;
using System.Net.WebSockets;

#nullable enable

namespace OOs.Net.Connections;

public sealed class ServerWebSocketTransportConnection(WebSocket webSocket,
    IPEndPoint localEndPoint, IPEndPoint remoteEndPoint,
    PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) :
    WebSocketTransportConnection(webSocket, localEndPoint, remoteEndPoint, inputPipeOptions, outputPipeOptions)
{
    public override string ToString() => $"{Id}-WS ({RemoteEndPoint})";
}