using System.Net.Transports.Exceptions;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Transports
{
    public abstract class WebSocketConnection<TWebSocket> : INetworkConnection where TWebSocket : WebSocket
    {
        protected TWebSocket Socket;

        protected WebSocketConnection(TWebSocket socket)
        {
            Socket = socket;
        }

        #region Implementation of IAsyncDisposable

        public async ValueTask DisposeAsync()
        {
            using(Socket)
            {
                await DisconnectAsync().ConfigureAwait(false);
            }
        }

        #endregion

        #region Implementation of IConnectedObject

        public bool IsConnected => Socket?.State == WebSocketState.Open;

        public abstract Task ConnectAsync(CancellationToken cancellationToken = default);

        public virtual async Task DisconnectAsync()
        {
            var state = Socket.State;

            if(state == WebSocketState.Open || state == WebSocketState.CloseReceived || state == WebSocketState.CloseSent)
            {
                await Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Good bye.", default).ConfigureAwait(false);
            }
        }

        #endregion

        #region Implementation of INetworkTransport

        public async ValueTask<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            try
            {
                var vt = Socket.SendAsync(buffer, WebSocketMessageType.Binary, true, cancellationToken);

                if(!vt.IsCompletedSuccessfully) await vt.AsTask().ConfigureAwait(false);

                return buffer.Length;
            }
            catch(WebSocketException wse) when(wse.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            {
                await DisconnectAsync().ConfigureAwait(false);
                throw new ConnectionAbortedException(wse);
            }
        }

        public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            try
            {
                var vt = Socket.ReceiveAsync(buffer, cancellationToken);

                var result = vt.IsCompleted ? vt.GetAwaiter().GetResult() : await vt.AsTask().ConfigureAwait(false);

                if(result.MessageType != WebSocketMessageType.Close) return result.Count;

                await DisconnectAsync().ConfigureAwait(false);

                return 0;
            }
            catch(WebSocketException wse) when(wse.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            {
                await DisconnectAsync().ConfigureAwait(false);
                throw new ConnectionAbortedException(wse);
            }
        }

        #endregion
    }
}