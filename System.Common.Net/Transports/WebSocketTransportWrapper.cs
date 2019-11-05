using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.WebSockets.WebSocketCloseStatus;
using static System.Net.WebSockets.WebSocketMessageType;
using static System.Net.WebSockets.WebSocketState;

namespace System.Net.Transports
{
    public class WebSocketTransportWrapper : INetworkTransport
    {
        private readonly IPEndPoint remoteEndPoint;
        private readonly WebSocket socket;

        public WebSocketTransportWrapper(WebSocket webSocket, IPEndPoint remoteEndPoint)
        {
            socket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
            this.remoteEndPoint = remoteEndPoint;
        }

        public async ValueTask<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var vt = socket.SendAsync(buffer, Binary, true, cancellationToken);

            if(!vt.IsCompletedSuccessfully)
            {
                await vt.AsTask().ConfigureAwait(false);
            }

            return buffer.Length;
        }

        public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var vt = socket.ReceiveAsync(buffer, cancellationToken);

            return (vt.IsCompleted ? vt.GetAwaiter().GetResult() : await vt.AsTask().ConfigureAwait(false)).Count;
        }

        public async ValueTask DisposeAsync()
        {
            using(socket)
            {
                await DisconnectAsync().ConfigureAwait(false);
            }
        }

        public bool IsConnected => socket.State == Open;

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            return socket.CloseAsync(NormalClosure, "Good bye.", default);
        }

        public override string ToString()
        {
            return $"{nameof(WebSocketTransportWrapper)}: {remoteEndPoint}";
        }
    }
}