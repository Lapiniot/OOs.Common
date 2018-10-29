using System.Linq;
using System.Net.Transports;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Properties.Strings;
using static System.String;
using static System.StringSplitOptions;

namespace System.Net.Listeners
{
    public class WebSocketsListener : ConnectionListener
    {
        private const int ReceiveBufferSize = 16384;
        private const int KeepAliveSeconds = 120;
        private readonly TimeSpan keepAliveInterval;
        private readonly int receiveBufferSize;
        private readonly bool shouldMatchSubProtocol;
        private readonly string[] subProtocols;
        private readonly Uri uri;
        private HttpListener listener;

        public WebSocketsListener(Uri uri, string[] subProtocols,
            TimeSpan keepAliveInterval, int receiveBufferSize)
        {
            this.uri = uri ?? throw new ArgumentNullException(nameof(uri));
            this.subProtocols = subProtocols;
            this.keepAliveInterval = keepAliveInterval;
            this.receiveBufferSize = receiveBufferSize;
            shouldMatchSubProtocol = subProtocols != null && subProtocols.Length > 0;
        }

        public WebSocketsListener(Uri uri, params string[] subProtocols) :
            this(uri, subProtocols, TimeSpan.FromSeconds(KeepAliveSeconds), ReceiveBufferSize)
        {
        }

        public override async Task<INetworkTransport> AcceptAsync(CancellationToken cancellationToken)
        {
            var context = await listener.GetContextAsync().WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if(!context.Request.IsWebSocketRequest)
                {
                    throw new InvalidOperationException(WebSocketHandshakeExpected);
                }

                var subProtocol = MatchSubProtocol(context.Request);

                var socketContext = await context
                    .AcceptWebSocketAsync(subProtocol, receiveBufferSize, keepAliveInterval)
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);

                return new WebSocketsTransportWrapper(socketContext.WebSocket);
            }
            catch
            {
                context.Response.Close();
                throw;
            }
        }

        private string MatchSubProtocol(HttpListenerRequest request)
        {
            var header = request.Headers["Sec-WebSocket-Protocol"];

            if(!shouldMatchSubProtocol) return header;

            if(IsNullOrEmpty(header))
            {
                throw new ArgumentException(NoWsSubProtocolMessage);
            }

            var headers = header.Split(new[] {' ', ','}, RemoveEmptyEntries);
            var subProtocol = subProtocols.Intersect(headers).FirstOrDefault();
            if(subProtocol == null)
            {
                throw new ArgumentException(NotSupportedWsSubProtocolMessage);
            }

            return subProtocol;
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
    }
}