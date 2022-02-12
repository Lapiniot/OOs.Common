using System.Net.Connections.Exceptions;
using System.Net.WebSockets;
using static System.Net.WebSockets.WebSocketError;
using static System.Net.WebSockets.WebSocketState;
using static System.Net.WebSockets.WebSocketCloseStatus;

namespace System.Net.Connections;

public abstract class WebSocketConnection<TWebSocket> : NetworkConnection where TWebSocket : WebSocket
{
    private int disposed;
    private TWebSocket socket;

    protected WebSocketConnection(TWebSocket socket)
    {
        this.socket = socket;
    }

    protected TWebSocket Socket { get => socket; set => socket = value; }

    #region Implementation of IAsyncDisposable

    public override async ValueTask DisposeAsync()
    {
        if(Interlocked.CompareExchange(ref disposed, 1, 0) != 0)
        {
            return;
        }

        GC.SuppressFinalize(this);

        using(socket)
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }

    #endregion

    #region Implementation of INetworkConnection

    public override async ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        try
        {
            await socket.SendAsync(buffer, WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
        }
        catch(WebSocketException wse) when(
            wse.WebSocketErrorCode is ConnectionClosedPrematurely ||
            wse.WebSocketErrorCode is InvalidState && socket.State is Aborted or Closed)
        {
            throw new ConnectionClosedException(wse);
        }
    }

    public override async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        try
        {
            var result = await socket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);

            if(result.MessageType is not WebSocketMessageType.Close)
            {
                return result.Count;
            }

            await DisconnectAsync().ConfigureAwait(false);
            return 0;

        }
        catch(WebSocketException wse) when(
            wse.WebSocketErrorCode is ConnectionClosedPrematurely ||
            wse.WebSocketErrorCode is InvalidState && socket.State is Aborted or Closed)
        {
            throw new ConnectionClosedException(wse);
        }
    }

    #endregion

    protected override Task StoppingAsync()
    {
        return Socket switch
        {
            { State: Open } => Socket.CloseAsync(NormalClosure, "Good bye.", default),
            { State: CloseReceived, CloseStatus: NormalClosure } => Socket.CloseOutputAsync(NormalClosure, "Good bye.", default),
            _ => Task.CompletedTask
        };
    }
}