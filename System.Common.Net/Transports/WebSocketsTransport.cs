using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.WebSockets.WebSocketCloseStatus;
using static System.Net.WebSockets.WebSocketState;

namespace System.Net.Transports
{
    public class WebSocketsTransport : NetworkTransport
    {
        private readonly Uri remoteUri;
        private ClientWebSocket webSocket;

        public WebSocketsTransport(Uri remoteUri)
        {
            this.remoteUri = remoteUri;
        }

        public override async Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var result = await webSocket.ReceiveAsync(buffer, cancellationToken);

            if(result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(NormalClosure, string.Empty, cancellationToken).ConfigureAwait(false);

                return 0;
            }

            return result.Count;
        }

        public override async Task<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            await webSocket.SendAsync(buffer, WebSocketMessageType.Binary, true, cancellationToken);

            return buffer.Length;
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

        protected override Task OnConnectAsync(object options, CancellationToken cancellationToken)
        {
            try
            {
                webSocket = new ClientWebSocket();

                webSocket.Options.AddSubProtocol("mqttv3.1");
                webSocket.Options.AddSubProtocol("mqttv");

                return webSocket.ConnectAsync(remoteUri, cancellationToken);
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }

        protected override Task OnConnectedAsync(object options, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}