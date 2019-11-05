using System.Net.Http;
using System.Net.Sockets;
using System.Net.Transports.Exceptions;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Properties.Strings;
using static System.Net.WebSockets.WebSocketCloseStatus;
using static System.Net.WebSockets.WebSocketError;
using static System.Net.WebSockets.WebSocketMessageType;
using static System.Net.WebSockets.WebSocketState;

namespace System.Net.Transports
{
    public class WebSocketTransport : NetworkTransport
    {
        private ClientWebSocket socket;

        public WebSocketTransport(Uri remoteUri, params string[] subProtocols)
        {
            RemoteUri = remoteUri ?? throw new ArgumentNullException(nameof(remoteUri));
            SubProtocols = subProtocols ?? throw new ArgumentNullException(nameof(subProtocols));
            if(SubProtocols.Length == 0) throw new ArgumentException(NoWsSubProtocol);
        }

        public Uri RemoteUri { get; }

        public string[] SubProtocols { get; }

        public override async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            CheckConnected();
            try
            {
                var vt = socket.ReceiveAsync(buffer, cancellationToken);

                var result = vt.IsCompleted ? vt.GetAwaiter().GetResult() : await vt.AsTask().ConfigureAwait(false);

                if(result.MessageType != Close) return result.Count;

                await socket.CloseAsync(NormalClosure, "Good bye.", cancellationToken).ConfigureAwait(false);

                return 0;
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
                var vt = socket.SendAsync(buffer, Binary, true, cancellationToken);

                if(!vt.IsCompletedSuccessfully) await vt.AsTask().ConfigureAwait(false);

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
                var state = socket.State;

                if(state == Open || state == CloseReceived || state == CloseSent)
                {
                    await socket.CloseAsync(NormalClosure, "Good bye.", default).ConfigureAwait(false);
                }
            }
            catch
            {
                // ignored
            }

            socket = null;
        }

        protected override async Task OnConnectAsync(CancellationToken cancellationToken)
        {
            socket = new ClientWebSocket();

            foreach(var subProtocol in SubProtocols)
            {
                socket.Options.AddSubProtocol(subProtocol);
            }

            try
            {
                await socket.ConnectAsync(RemoteUri, cancellationToken).ConfigureAwait(false);
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

        public override string ToString()
        {
            return $"{nameof(WebSocketTransport)}: {(socket != null ? RemoteUri.ToString() : "Not Connected")}";
        }
    }
}