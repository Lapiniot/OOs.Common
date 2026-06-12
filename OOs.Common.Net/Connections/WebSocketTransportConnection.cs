using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.WebSockets;
using static System.Net.WebSockets.WebSocketCloseStatus;
using static System.Net.WebSockets.WebSocketState;

namespace OOs.Net.Connections;

public abstract class WebSocketTransportConnection : TransportConnectionPipeAdapter
{
    private readonly WebSocket webSocket;

    protected WebSocketTransportConnection(WebSocket webSocket,
        EndPoint? localEndPoint, EndPoint? remoteEndPoint,
        PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) :
        base(inputPipeOptions, outputPipeOptions)
    {
        ArgumentNullException.ThrowIfNull(webSocket);

        this.webSocket = webSocket;
        LocalEndPoint = localEndPoint;
        RemoteEndPoint = remoteEndPoint;
    }

    public sealed override string Id { get; } = Base32.ToBase32String(CorrelationIdGenerator.GetNext());
    public override EndPoint? LocalEndPoint { get; }
    public override EndPoint? RemoteEndPoint { get; }
    protected WebSocket WebSocket => webSocket;

    protected override ValueTask OnStartingAsync(CancellationToken cancellationToken) => default;

    protected override ValueTask OnStoppingAsync() => webSocket switch
    {
        { State: Open } =>
            new ValueTask(webSocket.CloseAsync(NormalClosure, "Good bye.", default)),
        { State: CloseReceived, CloseStatus: NormalClosure } =>
            new ValueTask(webSocket.CloseOutputAsync(NormalClosure, "Good bye.", default)),
        _ => default
    };

    protected sealed override async ValueTask<int> ReceiveAsync(Memory<byte> buffer)
    {
        var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);

        if (result.MessageType is not WebSocketMessageType.Close)
        {
            return result.Count;
        }

        if (webSocket is { State: CloseReceived, CloseStatus: NormalClosure })
        {
            await webSocket.CloseOutputAsync(NormalClosure, "Good bye.", default).ConfigureAwait(false);
        }

        return 0;
    }

    protected sealed override ValueTask SendAsync(ref readonly ReadOnlySequence<byte> buffer)
    {
        return buffer.IsSingleSegment
            ? webSocket.SendAsync(buffer.First, WebSocketMessageType.Binary, true, CancellationToken.None)
            : SendAsync(buffer);
    }

    private async ValueTask SendAsync(ReadOnlySequence<byte> buffer)
    {
        var position = buffer.Start;
        while (buffer.TryGet(ref position, out var memory))
        {
            await webSocket.SendAsync(memory, WebSocketMessageType.Binary, true, CancellationToken.None).ConfigureAwait(false);
        }
    }

    protected override ValueTask ShutdownAsync(ShutdownDirection direction)
    {
        switch (direction)
        {
            case ShutdownDirection.Send:
                return new ValueTask(webSocket.CloseOutputAsync(NormalClosure, "Good bye.", default));
            case ShutdownDirection.Receive or ShutdownDirection.Both:
                return new ValueTask(webSocket.CloseAsync(NormalClosure, "Good bye.", default));
            default:
                ThrowInvalidShutdownDirection<object>(direction);
                return default;
        }
    }

    public override void Abort()
    {
        webSocket.Abort();
        base.Abort();
    }

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        using (webSocket)
        {
            await base.DisposeAsync().AsTask().ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }
}