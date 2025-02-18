using System.IO.Pipelines;
using System.Net;
using System.Net.WebSockets;
using static System.Net.WebSockets.WebSocketCloseStatus;
using static System.Net.WebSockets.WebSocketError;
using static System.Net.WebSockets.WebSocketState;

namespace OOs.Net.Connections;

public abstract class WebSocketTransportConnection : TransportConnectionPipeAdapter
{
    private readonly WebSocket webSocket;

    protected WebSocketTransportConnection(WebSocket webSocket,
        IPEndPoint localEndPoint, IPEndPoint remoteEndPoint,
        PipeOptions inputPipeOptions = null, PipeOptions outputPipeOptions = null) :
        base(inputPipeOptions, outputPipeOptions)
    {
        ArgumentNullException.ThrowIfNull(webSocket);
        ArgumentNullException.ThrowIfNull(localEndPoint);
        ArgumentNullException.ThrowIfNull(remoteEndPoint);

        this.webSocket = webSocket;
        LocalEndPoint = localEndPoint;
        RemoteEndPoint = remoteEndPoint;
    }

    public sealed override string Id { get; } = Base32.ToBase32String(CorrelationIdGenerator.GetNext());
    public override EndPoint LocalEndPoint { get; }
    public override EndPoint RemoteEndPoint { get; }
    protected WebSocket WebSocket => webSocket;

    protected override ValueTask OnStartingAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    protected override ValueTask OnStoppingAsync() => webSocket switch
    {
        { State: Open } =>
            new ValueTask(webSocket.CloseAsync(NormalClosure, "Good bye.", default)),
        { State: CloseReceived, CloseStatus: NormalClosure } =>
            new ValueTask(webSocket.CloseOutputAsync(NormalClosure, "Good bye.", default)),
        _ => ValueTask.CompletedTask
    };

    protected override async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        try
        {
            var result = await webSocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);

            if (result.MessageType is not WebSocketMessageType.Close)
                return result.Count;

            if (webSocket is { State: CloseReceived, CloseStatus: NormalClosure })
                await webSocket.CloseOutputAsync(NormalClosure, "Good bye.", default).ConfigureAwait(false);
        }
        catch (WebSocketException wse) when (
            wse.WebSocketErrorCode is ConnectionClosedPrematurely ||
            wse.WebSocketErrorCode is InvalidState && webSocket.State is Aborted or Closed)
        {
            ThrowHelper.ThrowConnectionClosed(wse);
        }

        return 0;
    }

    protected override async ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        try
        {
            await webSocket.SendAsync(buffer, WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
        }
        catch (WebSocketException wse) when (
            wse.WebSocketErrorCode is ConnectionClosedPrematurely ||
            wse.WebSocketErrorCode is InvalidState && webSocket.State is Aborted or Closed)
        {
            ThrowHelper.ThrowConnectionClosed(wse);
        }
    }

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        using (webSocket)
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}