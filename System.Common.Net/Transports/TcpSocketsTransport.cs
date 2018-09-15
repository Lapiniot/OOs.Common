using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Sockets.AddressFamily;
using static System.Net.Sockets.ProtocolType;
using static System.Net.Sockets.SocketType;

namespace System.Net.Transports
{
    public class TcpSocketsTransport : NetworkTransport
    {
        private Socket socket;

        public TcpSocketsTransport(IPEndPoint ipEndPoint)
        {
            RemoteEndPoint = ipEndPoint ?? throw new ArgumentNullException(nameof(ipEndPoint));
        }

        public IPEndPoint RemoteEndPoint { get; }

        public override Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            return socket.ReceiveAsync(buffer, cancellationToken);
        }

        public override Task<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            return socket.SendAsync(buffer, cancellationToken);
        }

        protected override async Task OnCloseAsync()
        {
            socket.Shutdown(SocketShutdown.Both);

            IAsyncResult BeginDisconnect(AsyncCallback ar, object state)
            {
                return socket.BeginDisconnect(false, ar, state);
            }

            await Task.Factory.FromAsync(BeginDisconnect, socket.EndDisconnect, null).ConfigureAwait(false);

            socket.Close();

            socket = null;
        }

        protected override Task OnConnectAsync(CancellationToken cancellationToken)
        {
            socket = new Socket(InterNetwork, Stream, Tcp);

            return socket.ConnectAsync(RemoteEndPoint);
        }

        protected override Task OnConnectedAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}