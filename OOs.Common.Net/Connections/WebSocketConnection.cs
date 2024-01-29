using System.Net.WebSockets;
using static System.Net.WebSockets.WebSocketError;
using static System.Net.WebSockets.WebSocketState;
using static System.Net.WebSockets.WebSocketCloseStatus;

namespace OOs.Net.Connections;

public abstract class WebSocketConnection<TWebSocket>(TWebSocket socket) : NetworkConnection where TWebSocket : WebSocket
{
    private int disposed;

    protected TWebSocket Socket { get => socket; set => socket = value; }

    #region Implementation of IAsyncDisposable

    public sealed override async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;

        GC.SuppressFinalize(this);

        using (socket)
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }

    #endregion

    protected override Task StoppingAsync() =>
        Socket switch
        {
            { State: Open } => socket.CloseAsync(NormalClosure, "Good bye.", default),
            { State: CloseReceived, CloseStatus: NormalClosure } => socket.CloseOutputAsync(NormalClosure, "Good bye.", default),
            _ => Task.CompletedTask
        };

    #region Implementation of INetworkConnection

    public sealed override async ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        try
        {
            await socket.SendAsync(buffer, WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
        }
        catch (WebSocketException wse) when (
            wse.WebSocketErrorCode is ConnectionClosedPrematurely ||
            wse.WebSocketErrorCode is InvalidState && socket.State is Aborted or Closed)
        {
            ThrowConnectionClosed(wse);
        }
    }

    public sealed override async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        try
        {
            var result = await socket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);

            if (result.MessageType is not WebSocketMessageType.Close)
                return result.Count;

            if (socket is { State: CloseReceived, CloseStatus: NormalClosure })
                await socket.CloseOutputAsync(NormalClosure, "Good bye.", default).ConfigureAwait(false);
        }
        catch (WebSocketException wse) when (
            wse.WebSocketErrorCode is ConnectionClosedPrematurely ||
            wse.WebSocketErrorCode is InvalidState && socket.State is Aborted or Closed)
        {
            ThrowConnectionClosed(wse);
        }

        return 0;
    }

    #endregion
}