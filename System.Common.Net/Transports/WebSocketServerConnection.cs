using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Transports
{
    public class WebSocketServerConnection : WebSocketConnection<WebSocket>
    {
        private readonly IPEndPoint remoteEndPoint;

        public WebSocketServerConnection(WebSocket acceptedWebSocket, IPEndPoint remoteEndPoint) : base(acceptedWebSocket)
        {
            this.remoteEndPoint = remoteEndPoint;
        }

        public override string ToString()
        {
            return $"{nameof(WebSocketServerConnection)}: {remoteEndPoint}";
        }

        #region Overrides of WebSocketTransportBase

        public override Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        #endregion
    }
}