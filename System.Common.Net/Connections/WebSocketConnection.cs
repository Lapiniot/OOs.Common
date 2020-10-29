using System.Net.Connections.Exceptions;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.WebSockets.WebSocketError;
using static System.Net.WebSockets.WebSocketState;
using static System.Net.WebSockets.WebSocketCloseStatus;

namespace System.Net.Connections
{
    public abstract class WebSocketConnection<TWebSocket> : INetworkConnection where TWebSocket : WebSocket
    {
        private int disposed;
        private TWebSocket socket;

        protected WebSocketConnection(TWebSocket socket)
        {
            this.socket = socket;
        }

        #region Implementation of IAsyncDisposable

        public virtual async ValueTask DisposeAsync()
        {
            if(Interlocked.CompareExchange(ref disposed, 1, 0) != 0) return;

            using(socket)
            {
                GC.SuppressFinalize(this);
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

        public bool IsConnected => socket?.State == Open;

        public abstract Task ConnectAsync(CancellationToken cancellationToken = default);

        public virtual async Task DisconnectAsync()
        {
            var state = socket.State;
            if(state == Open || state == CloseReceived && socket.CloseStatus == NormalClosure)
            {
                await socket.CloseAsync(NormalClosure, "Good bye.", default).ConfigureAwait(false);
            }
        }

        #endregion

        #region Implementation of INetworkTransport

        public async ValueTask<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            try
            {
                var vt = socket.SendAsync(buffer, WebSocketMessageType.Binary, true, cancellationToken);

                if(!vt.IsCompletedSuccessfully) await vt.ConfigureAwait(false);

                return buffer.Length;
            }
            catch(WebSocketException wse) when(wse.WebSocketErrorCode == ConnectionClosedPrematurely)
            {
                throw new ConnectionAbortedException(wse);
            }
        }

        public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            try
            {
                var vt = socket.ReceiveAsync(buffer, cancellationToken);

                var result = vt.IsCompletedSuccessfully ? vt.Result : await vt.ConfigureAwait(false);

                if(result.MessageType != WebSocketMessageType.Close) return result.Count;

                await DisconnectAsync().ConfigureAwait(false);

                return 0;
            }
            catch(WebSocketException wse) when(wse.WebSocketErrorCode == ConnectionClosedPrematurely)
            {
                throw new ConnectionAbortedException(wse);
            }
        }

        #endregion
    }
}