using System.Net.Http;
using System.Net.Sockets;
using System.Net.Transports.Exceptions;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.WebSockets.WebSocketCloseStatus;
using static System.Net.WebSockets.WebSocketError;
using static System.Net.WebSockets.WebSocketMessageType;
using static System.Net.WebSockets.WebSocketState;

namespace System.Net.Transports
{
    public class WebSocketsTransport : NetworkTransport
    {
        private ClientWebSocket webSocket;

        public WebSocketsTransport(Uri remoteUri, params string[] subProtocols)
        {
            RemoteUri = remoteUri ?? throw new ArgumentNullException(nameof(remoteUri));
            SubProtocols = subProtocols ?? throw new ArgumentNullException(nameof(subProtocols));
            if(SubProtocols.Length == 0) throw new ArgumentException("At least one sub-protocol name must be provided");
        }

        public Uri RemoteUri { get; }

        public string[] SubProtocols { get; }

        public override async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            CheckConnected();
            try
            {
                var task = webSocket.ReceiveAsync(buffer, cancellationToken);

                var result = task.IsCompletedSuccessfully
                    ? task.Result
                    : await task.ConfigureAwait(false);

                if(result.MessageType == Close)
                {
                    await webSocket.CloseAsync(NormalClosure, string.Empty, cancellationToken).ConfigureAwait(false);

                    return 0;
                }

                return result.Count;
            }
            catch(WebSocketException wse) when(wse.WebSocketErrorCode == ConnectionClosedPrematurely)
            {
                await DisconnectAsync().ConfigureAwait(false);
                throw new ConnectionAbortedException(wse);
            }
        }

        public override async ValueTask<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            CheckConnected();
            try
            {
                var task = webSocket.SendAsync(buffer, Binary, true, cancellationToken);

                if(!task.IsCompletedSuccessfully) await task.ConfigureAwait(false);

                return buffer.Length;
            }
            catch(WebSocketException wse) when(wse.WebSocketErrorCode == ConnectionClosedPrematurely)
            {
                await DisconnectAsync().ConfigureAwait(false);
                throw new ConnectionAbortedException(wse);
            }
        }

        protected override async Task OnDisconnectAsync()
        {
            try
            {
                if(webSocket.State == Open || webSocket.State == CloseReceived)
                {
                    await webSocket.CloseAsync(NormalClosure, string.Empty, default).ConfigureAwait(false);
                }
            }
            catch
            {
                // ignored
            }
        }

        protected override async Task OnConnectAsync(CancellationToken cancellationToken)
        {
            webSocket = new ClientWebSocket();

            foreach(var subProtocol in SubProtocols)
            {
                webSocket.Options.AddSubProtocol(subProtocol);
            }

            try
            {
                await webSocket.ConnectAsync(RemoteUri, cancellationToken).ConfigureAwait(false);
            }
            catch(WebSocketException wse) when(
                wse.InnerException is HttpRequestException hre &&
                hre.InnerException is SocketException se &&
                se.SocketErrorCode == SocketError.HostNotFound)
            {
                throw new HostNotFoundException(wse);
            }
            catch(WebSocketException wse)
            {
                throw new ServerUnavailableException(wse);
            }
        }
    }
}