using System.Collections.Generic;
using System.Net.Connections;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Listeners
{
    public sealed class TcpSocketListener : IAsyncEnumerable<INetworkConnection>
    {
        private readonly int backlog;
        private readonly IPEndPoint ipEndPoint;

        public TcpSocketListener(IPEndPoint ipEndPoint, int backlog = 100)
        {
            this.ipEndPoint = ipEndPoint;
            this.backlog = backlog;
        }

        #region Implementation of IAsyncEnumerable<out INetworkConnection>

        public IAsyncEnumerator<INetworkConnection> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TcpSocketEnumerator(ipEndPoint, backlog, cancellationToken);
        }

        #endregion

        public override string ToString()
        {
            return $"{nameof(TcpSocketListener)}: tcp://{ipEndPoint}";
        }

        private class TcpSocketEnumerator : IAsyncEnumerator<INetworkConnection>
        {
            private readonly CancellationToken cancellationToken;
            private readonly Socket socket;
            private INetworkConnection current;

            public TcpSocketEnumerator(IPEndPoint endPoint, in int backlog, CancellationToken cancellationToken)
            {
                if(endPoint == null) throw new ArgumentNullException(nameof(endPoint));
                this.cancellationToken = cancellationToken;

                socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(endPoint);
                socket.Listen(backlog);
            }

            #region Implementation of IAsyncDisposable

            public ValueTask DisposeAsync()
            {
                socket.Close();
                return default;
            }

            #endregion

            #region Implementation of IAsyncEnumerator<out INetworkConnection>

            public async ValueTask<bool> MoveNextAsync()
            {
                try
                {
                    var acceptedSocket = await socket.AcceptAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
                    current = new TcpSocketServerConnection(acceptedSocket);
                    return true;
                }
                catch(OperationCanceledException)
                {
                    return false;
                }
            }

            public INetworkConnection Current => current;

            #endregion
        }
    }
}