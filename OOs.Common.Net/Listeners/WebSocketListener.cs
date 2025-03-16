using System.Net;
using OOs.Net.Connections;

namespace OOs.Net.Listeners;

public sealed class WebSocketListener : IAsyncEnumerable<TransportConnection>, IAsyncDisposable
{
#if NET9_0_OR_GREATER
    private static readonly System.Buffers.SearchValues<char> Separators = System.Buffers.SearchValues.Create(' ', ',');
#endif
    private const int ReceiveBufferSize = 16384;
    private const int KeepAliveSeconds = 120;
    private readonly TimeSpan keepAliveInterval;
    private readonly string[] prefixes;
    private readonly int receiveBufferSize;
    private readonly HttpListener listener;
    private readonly string[] subProtocols;

    public WebSocketListener(string[] prefixes, string[] subProtocols, TimeSpan keepAliveInterval, int receiveBufferSize)
    {
        ArgumentNullException.ThrowIfNull(prefixes);
        ArgumentNullException.ThrowIfNull(subProtocols);

        this.prefixes = prefixes;
        this.subProtocols = subProtocols;
        this.keepAliveInterval = keepAliveInterval;
        this.receiveBufferSize = receiveBufferSize;
        listener = new HttpListener();
        foreach (var prefix in prefixes)
        {
            listener.Prefixes.Add(prefix);
        }
    }

    public WebSocketListener(string[] prefixes, string[] subProtocols) :
        this(prefixes, subProtocols, TimeSpan.FromSeconds(KeepAliveSeconds), ReceiveBufferSize)
    { }

    public override string ToString() => $"{nameof(WebSocketListener)} ({string.Join(";", prefixes)}) ({string.Join(";", subProtocols)})";
    public async IAsyncEnumerator<TransportConnection> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        var shouldMatchSubProtocol = subProtocols is { Length: > 0 };
        listener.Start();

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var context = await listener.GetContextAsync().WaitAsync(cancellationToken).ConfigureAwait(false);

                if (!context.Request.IsWebSocketRequest)
                {
                    CloseResponse(context.Response, "Only Web Socket handshake requests are supported.");
                    continue;
                }

                var clientSubProtocol = context.Request.Headers["Sec-WebSocket-Protocol"];

                if (shouldMatchSubProtocol)
                {
                    if (string.IsNullOrEmpty(clientSubProtocol))
                    {
                        CloseResponse(context.Response, "At least one sub-protocol must be specified.");
                        continue;
                    }

                    clientSubProtocol = MatchSubProtocol(clientSubProtocol);
                    if (clientSubProtocol is null)
                    {
                        CloseResponse(context.Response, "Not supported sub-protocol(s).");
                        continue;
                    }
                }

                ServerWebSocketTransportConnection connection = null;

                try
                {
                    var ctx = await context.AcceptWebSocketAsync(clientSubProtocol, receiveBufferSize, keepAliveInterval)
                        .WaitAsync(cancellationToken).ConfigureAwait(false);
#pragma warning disable CA2000 // Dispose objects before losing scope
                    connection = new(ctx.WebSocket, context.Request.LocalEndPoint, context.Request.RemoteEndPoint);
#pragma warning restore CA2000 // Dispose objects before losing scope
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    await using (connection)
                    {
                        CloseResponse(context.Response);
                    }

                    connection = null;
                }

                if (connection is not null)
                {
                    yield return connection;
                }
            }
        }
        finally
        {
            listener.Stop();
        }

        static void CloseResponse(HttpListenerResponse response, string statusDescription = "Bad request", int statusCode = 400)
        {
            response.StatusCode = statusCode;
            response.StatusDescription = statusDescription;
            response.Close();
        }

#if NET9_0_OR_GREATER
        string MatchSubProtocol(string clientSubProtocols)
        {
            var span = clientSubProtocols.AsSpan();
            foreach (var range in span.SplitAny(Separators))
            {
                var clientSubProtocol = span[range];
                foreach (var subProtocol in subProtocols)
                {
                    if (subProtocol.AsSpan().SequenceEqual(clientSubProtocol))
                    {
                        return subProtocol;
                    }
                }
            }

            return null;
        }
#else
        string MatchSubProtocol(string clientSubProtocols) => subProtocols
            .Intersect(clientSubProtocols.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries))
            .FirstOrDefault();
#endif
    }

    public ValueTask DisposeAsync()
    {
        listener.Close();
        return default;
    }
}