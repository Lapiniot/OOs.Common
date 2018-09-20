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

        public TcpSocketsTransport(string host, int port)
        {
            if(host == null) throw new ArgumentNullException(nameof(host));
            if(host == "") throw new ArgumentException("Value cannot be empty.", nameof(host));

            var addresses = Dns.GetHostAddresses(host);
            if(addresses == null || addresses.Length == 0) throw new ArgumentException("Host name cannot be resolved.");

            RemoteEndPoint = new IPEndPoint(addresses[0], port);
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