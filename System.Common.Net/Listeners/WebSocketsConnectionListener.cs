using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Listeners
{
    public class WebSocketsConnectionListener : ConnectionListener
    {
        private readonly TimeSpan keepAliveInterval;
        private readonly int receiveBufferSize;
        private readonly string[] subProtocols;
        private readonly Uri uri;
        private HttpListener listener;

        public WebSocketsConnectionListener(Uri uri, string[] subProtocols, TimeSpan keepAliveInterval, int receiveBufferSize)
        {
            this.uri = uri;
            this.subProtocols = subProtocols;
            this.keepAliveInterval = keepAliveInterval;
            this.receiveBufferSize = receiveBufferSize;
        }

        public WebSocketsConnectionListener(Uri uri, params string[] subProtocols) :
            this(uri, subProtocols, TimeSpan.FromMinutes(2), 16384)
        {
            this.subProtocols = subProtocols;
            this.uri = uri;
        }

        public override async Task<INetworkTransport> AcceptAsync(CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                var context = await listener.GetContextAsync().WaitAsync(cancellationToken).ConfigureAwait(false);

                if(context.Request.IsWebSocketRequest)
                {
                    var protocolHeader = context.Request.Headers["Sec-WebSocket-Protocol"];
                    var subprotocol = subProtocols.Intersect(protocolHeader.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)).FirstOrDefault();
                    if(subprotocol == null) throw new ArgumentException("Not supported sub-protocol(s).");

                    var socketContext = await context
                        .AcceptWebSocketAsync(subprotocol, receiveBufferSize, keepAliveInterval)
                        .WaitAsync(cancellationToken)
                        .ConfigureAwait(false);

                    return new WebSocketsTransportWrapper(socketContext.WebSocket);
                }

                context.Response.Abort();
            }

            throw new InvalidOperationException();
        }

        protected override void OnStartListening()
        {
            listener = new HttpListener();
            listener.Prefixes.Add(uri.AbsoluteUri);
            listener.Start();
        }

        protected override void OnStopListening()
        {
            using(listener)
            {
                listener.Stop();
                listener.Abort();
            }
        }

        private class WebSocketsTransportWrapper : INetworkTransport
        {
            private readonly WebSocket webSocket;

            public WebSocketsTransportWrapper(WebSocket webSocket)
            {
                this.webSocket = webSocket;
            }

            public async Task<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken)
            {
                await webSocket.SendAsync(buffer, WebSocketMessageType.Binary, true, cancellationToken);

                return buffer.Length;
            }

            public async Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
            {
                var result = await webSocket.ReceiveAsync(buffer, cancellationToken);

                return result.Count;
            }

            public void Dispose()
            {
                webSocket.Dispose();
            }

            public bool IsConnected => webSocket.State == WebSocketState.Open;

            public Task ConnectAsync(CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task DisconnectAsync()
            {
                return webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnected", default);
            }
        }
    }
}