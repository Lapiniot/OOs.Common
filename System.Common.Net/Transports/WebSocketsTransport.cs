using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.WebSockets.WebSocketCloseStatus;
using static System.Net.WebSockets.WebSocketState;
using static System.Threading.Tasks.Task;

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

        public override async Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            try
            {
                var result = await webSocket.ReceiveAsync(buffer, cancellationToken);

                if(result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(NormalClosure, string.Empty, cancellationToken).ConfigureAwait(false);

                    return 0;
                }

                return result.Count;
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }

        public override async Task<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            try
            {
                await webSocket.SendAsync(buffer, WebSocketMessageType.Binary, true, cancellationToken);

                return buffer.Length;
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }

        protected override async Task OnCloseAsync()
        {
            try
            {
                if(webSocket.State == Open || webSocket.State == CloseReceived)
                {
                    await webSocket.CloseAsync(NormalClosure, "CLOSE", default).ConfigureAwait(false);
                }
            }
            catch
            {
                // ignored
            }
        }

        protected override Task OnConnectAsync(CancellationToken cancellationToken)
        {
            webSocket = new ClientWebSocket();

            foreach(var subProtocol in SubProtocols)
            {
                webSocket.Options.AddSubProtocol(subProtocol);
            }

            return webSocket.ConnectAsync(RemoteUri, cancellationToken);
        }

        protected override Task OnConnectedAsync(CancellationToken cancellationToken)
        {
            return CompletedTask;
        }
    }
}