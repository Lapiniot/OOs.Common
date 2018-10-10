using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Listeners
{
    public sealed class TcpSocketConnectionListener : ConnectionListener
    {
        private readonly IPEndPoint ipEndPoint;
        private readonly int maxConnections;
        private Socket socket;

        public TcpSocketConnectionListener(IPEndPoint ipEndPoint, int maxConnections = 100)
        {
            this.ipEndPoint = ipEndPoint;
            this.maxConnections = maxConnections;
        }

        public override async Task<INetworkTransport> AcceptAsync(CancellationToken cancellationToken)
        {
            using(cancellationToken.Register(socket.Close))
            {
                Socket connectedSocket = null;
                try
                {
                    connectedSocket = await socket.AcceptAsync().ConfigureAwait(false);

                    cancellationToken.ThrowIfCancellationRequested();

                    return new TcpSocketsTransportWrapper(connectedSocket);
                }
                catch
                {
                    connectedSocket?.Close();
                    throw;
                }
            }
        }

        protected override void OnStartListening()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(ipEndPoint);
            socket.Listen(maxConnections);
        }

        protected override void OnStopListening()
        {
            socket.Close();
            socket = null;
        }

        private class TcpSocketsTransportWrapper : INetworkTransport
        {
            private readonly Socket socket;

            public TcpSocketsTransportWrapper(Socket socket)
            {
                this.socket = socket;
            }

            public Task<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken)
            {
                return socket.SendAsync(buffer, cancellationToken);
            }

            public Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
            {
                return socket.ReceiveAsync(buffer, cancellationToken);
            }
        }
    }
}