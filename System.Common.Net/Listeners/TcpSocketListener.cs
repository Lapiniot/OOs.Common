using System.Collections.Generic;
using System.Net.Sockets;
using System.Net.Transports;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Sockets.ProtocolType;
using static System.Net.Sockets.SocketType;

namespace System.Net.Listeners
{
    public sealed class TcpSocketListener : AsyncConnectionListener
    {
        private readonly int backlog;
        private readonly IPEndPoint ipEndPoint;

        public TcpSocketListener(IPEndPoint ipEndPoint, int backlog = 100)
        {
            this.ipEndPoint = ipEndPoint;
            this.backlog = backlog;
        }

        protected override async IAsyncEnumerable<INetworkTransport> GetAsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var socket = new Socket(ipEndPoint.AddressFamily, Stream, Tcp);
            await using var _ = cancellationToken.Register(socket.Close).ConfigureAwait(false);

            socket.Bind(ipEndPoint);
            socket.Listen(backlog);

            while(!cancellationToken.IsCancellationRequested)
            {
                Socket connectedSocket = null;

                try
                {
                    connectedSocket = await socket.AcceptAsync().ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }

                if(connectedSocket != null) yield return new TcpSocketsTransportWrapper(connectedSocket);
            }
        }

        public override string ToString()
        {
            return $"{nameof(TcpSocketListener)}: tcp://{ipEndPoint}";
        }
    }
}