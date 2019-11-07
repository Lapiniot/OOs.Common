using System.Net.Connections.Exceptions;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Connections
{
    public abstract class WebSocketConnection<TWebSocket> : INetworkConnection where TWebSocket : WebSocket
    {
        private TWebSocket socket;

        protected WebSocketConnection(TWebSocket socket)
        {
            this.socket = socket;
        }

        #region Implementation of IAsyncDisposable

        public async ValueTask DisposeAsync()
        {
            using(socket)
            {
                await DisconnectAsync().ConfigureAwait(false);
            }
        }

        #endregion

        protected void SetWebSocket(TWebSocket webSocket)
        {
            socket = webSocket;
        }

        public override string ToString()
        {
            return $"{GetType().Name} (SubProtocol:'{socket?.SubProtocol}'; State: {socket?.State})";
        }

        #region Implementation of IConnectedObject

        public bool IsConnected => socket?.State == WebSocketState.Open;

        public abstract Task ConnectAsync(CancellationToken cancellationToken = default);

        public virtual async Task DisconnectAsync()
        {
            var state = socket.State;

            if(state == WebSocketState.Open || state == WebSocketState.CloseReceived || state == WebSocketState.CloseSent)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Good bye.", default).ConfigureAwait(false);
            }
        }

        #endregion

        #region Implementation of INetworkTransport

        public async ValueTask<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            try
            {
                var vt = socket.SendAsync(buffer, WebSocketMessageType.Binary, true, cancellationToken);

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
                var vt = socket.ReceiveAsync(buffer, cancellationToken);

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