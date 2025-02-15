﻿using System.Net;
using OOs.Net.Connections;
using OOs.Net.Pipelines;

namespace OOs.Net.Listeners;

public sealed class WebSocketListener : IAsyncEnumerable<NetworkTransportPipe>
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

    public override string ToString() => $"{nameof(WebSocketListener)} ({string.Join(";", prefixes)}) ({string.Join(";", subProtocols)})";

    #region Implementation of IAsyncEnumerable<out INetworkConnection>

    public IAsyncEnumerator<NetworkTransportPipe> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new WebSocketEnumerator(prefixes, subProtocols, keepAliveInterval, receiveBufferSize, cancellationToken);

    #endregion

    private sealed class WebSocketEnumerator : IAsyncEnumerator<NetworkTransportPipe>
    {
        private static readonly char[] Separators = [' ', ','];
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
            shouldMatchSubProtocol = subProtocols is { Length: > 0 };

            listener = new();
            foreach (var prefix in prefixes) listener.Prefixes.Add(prefix);
            listener.Start();
        }

        private static void Close(HttpListenerResponse response, string statusDescription = "Bad request", int statusCode = 400)
        {
            response.StatusCode = statusCode;
            response.StatusDescription = statusDescription;
            response.Close();
        }

        #region Implementation of IAsyncDisposable

        public ValueTask DisposeAsync()
        {
            listener.Close();
            return default;
        }

        #endregion

        #region Implementation of IAsyncEnumerator<out INetworkConnection>

        public async ValueTask<bool> MoveNextAsync()
        {
            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var context = await listener.GetContextAsync().WaitAsync(cancellationToken).ConfigureAwait(false);

                    if (!context.Request.IsWebSocketRequest)
                    {
                        Close(context.Response, "Only Web Socket handshake requests are supported.");
                        continue;
                    }

                    var subProtocol = context.Request.Headers["Sec-WebSocket-Protocol"];

                    if (shouldMatchSubProtocol)
                    {
                        if (string.IsNullOrEmpty(subProtocol))
                        {
                            Close(context.Response, "At least one sub-protocol must be specified.");
                            continue;
                        }

                        var headers = subProtocol.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
                        subProtocol = subProtocols.Intersect(headers).FirstOrDefault();
                        if (subProtocol is null)
                        {
                            Close(context.Response, "Not supported sub-protocol(s).");
                            continue;
                        }
                    }

                    try
                    {
                        var socketContext = await context.AcceptWebSocketAsync(subProtocol, receiveBufferSize, keepAliveInterval)
                            .WaitAsync(cancellationToken).ConfigureAwait(false);
#pragma warning disable CA2000 // Dispose objects before losing scope
                        Current = new(new WebSocketServerConnection(socketContext.WebSocket,
                            context.Request.LocalEndPoint, context.Request.RemoteEndPoint));
#pragma warning restore CA2000 // Dispose objects before losing scope
                        return true;
                    }
                    catch
                    {
                        Close(context.Response);
                        throw;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                listener.Stop();
            }

            return false;
        }

        public NetworkTransportPipe Current { get; private set; }

        #endregion
    }
}