using System.Net.Connections;
using System.Net.Properties;

namespace System.Net.Listeners;

public class WebSocketListener : IAsyncEnumerable<INetworkConnection>
{
    private const int ReceiveBufferSize = 16384;
    private const int KeepAliveSeconds = 120;
    private readonly TimeSpan keepAliveInterval;
    private readonly string[] prefixes;
    private readonly int receiveBufferSize;
    private readonly string[] subProtocols;

    public WebSocketListener(string[] prefixes, string[] subProtocols, TimeSpan keepAliveInterval, int receiveBufferSize)
    {
        ArgumentNullException.ThrowIfNull(prefixes);
        ArgumentNullException.ThrowIfNull(subProtocols);

        this.prefixes = prefixes;
        this.subProtocols = subProtocols;
        this.keepAliveInterval = keepAliveInterval;
        this.receiveBufferSize = receiveBufferSize;
    }

    public WebSocketListener(string[] prefixes, string[] subProtocols) :
        this(prefixes, subProtocols, TimeSpan.FromSeconds(KeepAliveSeconds), ReceiveBufferSize)
    { }

    #region Implementation of IAsyncEnumerable<out INetworkConnection>

    public IAsyncEnumerator<INetworkConnection> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new WebSocketEnumerator(prefixes, subProtocols, keepAliveInterval, receiveBufferSize, cancellationToken);
    }

    #endregion

    public override string ToString()
    {
        return $"{nameof(WebSocketListener)}: {string.Join(", ", prefixes)} ({string.Join(", ", subProtocols)})";
    }

    private class WebSocketEnumerator : IAsyncEnumerator<INetworkConnection>
    {
        private readonly CancellationToken cancellationToken;
        private readonly TimeSpan keepAliveInterval;
        private readonly HttpListener listener;
        private readonly int receiveBufferSize;
        private readonly bool shouldMatchSubProtocol;
        private readonly string[] subProtocols;

        public WebSocketEnumerator(string[] prefixes, string[] subProtocols, in TimeSpan keepAliveInterval,
            in int receiveBufferSize, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(prefixes);

            this.subProtocols = subProtocols;
            this.keepAliveInterval = keepAliveInterval;
            this.receiveBufferSize = receiveBufferSize;
            this.cancellationToken = cancellationToken;
            shouldMatchSubProtocol = subProtocols != null && subProtocols.Length > 0;

            listener = new HttpListener();
            foreach(var prefix in prefixes) listener.Prefixes.Add(prefix);
            listener.Start();
        }

        #region Implementation of IAsyncDisposable

        public ValueTask DisposeAsync()
        {
            listener.Close();
            return default;
        }

        #endregion

        private static void Close(HttpListenerResponse response, string statusDescription = "Bad request", int statusCode = 400)
        {
            response.StatusCode = statusCode;
            response.StatusDescription = statusDescription;
            response.Close();
        }

        #region Implementation of IAsyncEnumerator<out INetworkConnection>

        public async ValueTask<bool> MoveNextAsync()
        {
            try
            {
                while(!cancellationToken.IsCancellationRequested)
                {
                    var context = await listener.GetContextAsync().WaitAsync(cancellationToken).ConfigureAwait(false);

                    if(!context.Request.IsWebSocketRequest)
                    {
                        Close(context.Response, Strings.WebSocketHandshakeExpected);
                        continue;
                    }

                    var subProtocol = context.Request.Headers["Sec-WebSocket-Protocol"];

                    if(shouldMatchSubProtocol)
                    {
                        if(string.IsNullOrEmpty(subProtocol))
                        {
                            Close(context.Response, Strings.NoWsSubProtocol);
                            continue;
                        }

                        var headers = subProtocol.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                        subProtocol = subProtocols.Intersect(headers).FirstOrDefault();
                        if(subProtocol is null)
                        {
                            Close(context.Response, Strings.NotSupportedWsSubProtocol);
                            continue;
                        }
                    }

                    try
                    {
                        var socketContext = await context.AcceptWebSocketAsync(subProtocol, receiveBufferSize, keepAliveInterval)
                            .WaitAsync(cancellationToken).ConfigureAwait(false);
                        Current = new WebSocketServerConnection(socketContext.WebSocket, context.Request.RemoteEndPoint);
                        return true;
                    }
                    catch
                    {
                        Close(context.Response);
                        throw;
                    }
                }
            }
            catch(OperationCanceledException)
            {
                listener.Stop();
            }

            return false;
        }

        public INetworkConnection Current { get; private set; }

        #endregion
    }
}