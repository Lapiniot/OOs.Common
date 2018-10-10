using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Listeners
{
    public class WebSocketsConnectionListener : ConnectionListener
    {
        private readonly TimeSpan keepAliveInterval;
        private readonly int receiveBufferSize;
        private readonly string subProtocol;
        private readonly Uri uri;
        private HttpListener listener;

        public WebSocketsConnectionListener(Uri uri, string subProtocol, TimeSpan keepAliveInterval, int receiveBufferSize)
        {
            this.uri = uri;
            this.subProtocol = subProtocol;
            this.keepAliveInterval = keepAliveInterval;
            this.receiveBufferSize = receiveBufferSize;
        }

        public WebSocketsConnectionListener(Uri uri, string subProtocol) :
            this(uri, subProtocol, TimeSpan.FromMinutes(2), 16384)
        {
            this.subProtocol = subProtocol;
            this.uri = uri;
        }

        public override async Task<INetworkTransport> AcceptAsync(CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                var context = await listener.GetContextAsync().WaitAsync(cancellationToken).ConfigureAwait(false);

                if(context.Request.IsWebSocketRequest)
                {
                    var socketContext = await context
                        .AcceptWebSocketAsync(subProtocol, receiveBufferSize, keepAliveInterval)
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
        }
    }
}