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
        private readonly WebSocket webSocket;

        public WebSocketTransportWrapper(WebSocket webSocket, IPEndPoint remoteEndPoint)
        {
            this.webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
            this.remoteEndPoint = remoteEndPoint;
        }

        public async ValueTask<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var vt = webSocket.SendAsync(buffer, Binary, true, cancellationToken);

            if(vt.IsCompleted)
            {
                vt.GetAwaiter().GetResult();
            }
            else
            {
                await vt.AsTask().ConfigureAwait(false);
            }

            return buffer.Length;
        }

        public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var vt = webSocket.ReceiveAsync(buffer, cancellationToken);

            return (vt.IsCompletedSuccessfully ? vt.Result : await vt.AsTask().ConfigureAwait(false)).Count;
        }

        public void Dispose()
        {
            webSocket.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            webSocket.Dispose();
            return default;
        }

        public bool IsConnected => webSocket.State == Open;

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            return webSocket.CloseAsync(NormalClosure, "Good bye.", default);
        }

        public override string ToString()
        {
            return $"{nameof(WebSocketTransportWrapper)}: {remoteEndPoint}";
        }
    }
}