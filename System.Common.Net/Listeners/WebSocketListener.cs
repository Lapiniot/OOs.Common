using System.Collections.Generic;
using System.Linq;
using System.Net.Transports;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Properties.Strings;
using static System.String;
using static System.StringSplitOptions;

namespace System.Net.Listeners
{
    public class WebSocketListener : ConnectionListener
    {
        private const int ReceiveBufferSize = 16384;
        private const int KeepAliveSeconds = 120;
        private readonly TimeSpan keepAliveInterval;
        private readonly string[] prefixes;
        private readonly int receiveBufferSize;
        private readonly bool shouldMatchSubProtocol;
        private readonly string[] subProtocols;

        public WebSocketListener(string[] prefixes, string[] subProtocols,
            TimeSpan keepAliveInterval, int receiveBufferSize)
        {
            this.prefixes = prefixes ?? throw new ArgumentNullException(nameof(prefixes));
            this.subProtocols = subProtocols;
            this.keepAliveInterval = keepAliveInterval;
            this.receiveBufferSize = receiveBufferSize;
            shouldMatchSubProtocol = subProtocols != null && subProtocols.Length > 0;
        }

        public WebSocketListener(string[] prefixes, params string[] subProtocols) :
            this(prefixes, subProtocols, TimeSpan.FromSeconds(KeepAliveSeconds), ReceiveBufferSize) {}

        private string MatchSubProtocol(HttpListenerRequest request)
        {
            var header = request.Headers["Sec-WebSocket-Protocol"];

            if(!shouldMatchSubProtocol) return header;

            if(IsNullOrEmpty(header))
            {
                throw new ArgumentException(NoWsSubProtocol);
            }

            var headers = header.Split(new[] {' ', ','}, RemoveEmptyEntries);
            var subProtocol = subProtocols.Intersect(headers).FirstOrDefault();
            if(subProtocol == null)
            {
                throw new ArgumentException(NotSupportedWsSubProtocol);
            }

            return subProtocol;
        }

        protected override async IAsyncEnumerable<INetworkTransport> GetAsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var listener = new HttpListener();
            await using var _ = cancellationToken.Register(listener.Abort).ConfigureAwait(false);

            foreach(var prefix in prefixes)
            {
                listener.Prefixes.Add(prefix);
            }

            listener.Start();

            while(!cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext context = null;
                HttpListenerWebSocketContext socketContext = null;

                try
                {
                    context = await listener.GetContextAsync().ConfigureAwait(false);

                    if(!context.Request.IsWebSocketRequest) throw new InvalidOperationException(WebSocketHandshakeExpected);

                    var subProtocol = MatchSubProtocol(context.Request);

                    socketContext = await context.AcceptWebSocketAsync(subProtocol, receiveBufferSize, keepAliveInterval).ConfigureAwait(false);
                }
                catch
                {
                    context?.Response.Close();
                }

                if(socketContext != null) yield return new WebSocketTransportWrapper(socketContext.WebSocket, context.Request.RemoteEndPoint);
            }
        }

        public override string ToString()
        {
            return $"{nameof(WebSocketListener)}: {Join(", ", prefixes)} ({Join(", ", subProtocols)})";
        }
    }
}