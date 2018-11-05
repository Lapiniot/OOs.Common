using System.Net.Sockets;
using System.Net.Transports;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Sockets.ProtocolType;
using static System.Net.Sockets.SocketType;

namespace System.Net.Listeners
{
    public sealed class TcpSocketListener : ConnectionListener
    {
        private readonly int backlog;
        private readonly IPEndPoint ipEndPoint;
        private Socket socket;

        public TcpSocketListener(IPEndPoint ipEndPoint, int backlog = 100)
        {
            this.ipEndPoint = ipEndPoint ?? throw new ArgumentNullException(nameof(ipEndPoint));
            this.backlog = backlog;
        }

        public override async Task<INetworkTransport> AcceptAsync(CancellationToken cancellationToken)
        {
            using(cancellationToken.Register(socket.Close))
            {
                var connectedSocket = await socket.AcceptAsync().ConfigureAwait(false);

                return new TcpSocketsTransportWrapper(connectedSocket);
            }
        }

        protected override void OnStartListening()
        {
            socket = new Socket(ipEndPoint.AddressFamily, Stream, Tcp);
            socket.Bind(ipEndPoint);
            socket.Listen(backlog);
        }

        protected override void OnStopListening()
        {
            socket.Close();
            socket = null;
        }
    }
}