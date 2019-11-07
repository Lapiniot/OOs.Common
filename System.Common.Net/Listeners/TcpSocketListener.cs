using System.Collections.Generic;
using System.Net.Connections;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
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

        public TcpSocketListener(IPEndPoint ipEndPoint, int backlog = 100)
        {
            this.ipEndPoint = ipEndPoint;
            this.backlog = backlog;
        }

        protected override async IAsyncEnumerable<INetworkConnection> GetAsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var socket = new Socket(ipEndPoint.AddressFamily, Stream, Tcp);
            await using var _ = cancellationToken.Register(socket.Close).ConfigureAwait(false);

            socket.Bind(ipEndPoint);
            socket.Listen(backlog);

            while(!cancellationToken.IsCancellationRequested)
            {
                yield return new TcpSocketServerConnection(await socket.AcceptAsync().ConfigureAwait(false));
            }
        }

        public override string ToString()
        {
            return $"{nameof(TcpSocketListener)}: tcp://{ipEndPoint}";
        }
    }
}