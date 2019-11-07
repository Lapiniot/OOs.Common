using System.Collections.Generic;
using System.Linq;
using System.Net.Connections;
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

        protected override async IAsyncEnumerable<INetworkConnection> GetAsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken)
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
                var context = await listener.GetContextAsync().ConfigureAwait(false);

                if(!context.Request.IsWebSocketRequest)
                {
                    Close(context.Response, WebSocketHandshakeExpected);
                    continue;
                }

                var subProtocol = context.Request.Headers["Sec-WebSocket-Protocol"];

                if(shouldMatchSubProtocol)
                {
                    if(IsNullOrEmpty(subProtocol))
                    {
                        Close(context.Response, NoWsSubProtocol);
                        continue;
                    }

                    var headers = subProtocol.Split(new[] {' ', ','}, RemoveEmptyEntries);
                    subProtocol = subProtocols.Intersect(headers).FirstOrDefault();
                    if(subProtocol is null)
                    {
                        Close(context.Response, NotSupportedWsSubProtocol);
                        continue;
                    }
                }

                HttpListenerWebSocketContext socketContext;

                try
                {
                    socketContext = await context.AcceptWebSocketAsync(subProtocol, receiveBufferSize, keepAliveInterval).ConfigureAwait(false);
                }
                catch
                {
                    Close(context.Response);
                    throw;
                }

                yield return new WebSocketServerConnection(socketContext.WebSocket, context.Request.RemoteEndPoint);
            }
        }

        private static void Close(HttpListenerResponse response, string statusDescription = "Bad request", int statusCode = 400)
        {
            response.StatusCode = statusCode;
            response.StatusDescription = statusDescription;
            response.Close();
        }

        public override string ToString()
        {
            return $"{nameof(WebSocketListener)}: {Join(", ", prefixes)} ({Join(", ", subProtocols)})";
        }
    }
}