using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.WebSockets.WebSocketCloseStatus;
using static System.Net.WebSockets.WebSocketMessageType;
using static System.Net.WebSockets.WebSocketState;

namespace System.Net.Transports
{
    public class WebSocketsTransportWrapper : INetworkTransport
    {
        private readonly WebSocket webSocket;

        public WebSocketsTransportWrapper(WebSocket webSocket)
        {
            this.webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
        }

        public async ValueTask<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var task = webSocket.SendAsync(buffer, Binary, true, cancellationToken);

            if(!task.IsCompletedSuccessfully) await task.ConfigureAwait(false);

            return buffer.Length;
        }

        public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var task = webSocket.ReceiveAsync(buffer, cancellationToken);

            return (task.IsCompletedSuccessfully ? task.Result : await task.ConfigureAwait(false)).Count;
        }

        public void Dispose()
        {
            webSocket.Dispose();
        }

        public bool IsConnected => webSocket.State == Open;

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DisconnectAsync()
        {
            return webSocket.CloseAsync(NormalClosure, "Good bye.", default);
        }
    }
}