using System.Collections.Generic;
using System.Net.Connections;
using System.Net.Sockets;
using System.Threading;

namespace System.Net.Listeners
{
    public sealed class TcpSocketListener : IAsyncEnumerable<INetworkConnection>
    {
        private readonly int backlog;
        private readonly IPEndPoint endPoint;

        public TcpSocketListener(IPEndPoint endPoint, int backlog = 100)
        {
            this.endPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            this.backlog = backlog;
        }

        #region Implementation of IAsyncEnumerable<out INetworkConnection>

        public async IAsyncEnumerator<INetworkConnection> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            using var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(endPoint);
            socket.Listen(backlog);

            while(!cancellationToken.IsCancellationRequested)
            {
                Socket acceptedSocket = null;

                try
                {
                    acceptedSocket = await socket.AcceptAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch(OperationCanceledException)
                {
                    acceptedSocket?.Dispose();
                    yield break;
                }
                catch
                {
                    acceptedSocket?.Dispose();
                    throw;
                }

                yield return new TcpSocketServerConnection(acceptedSocket);
            }
        }

        #endregion

        public override string ToString()
        {
            return $"{nameof(TcpSocketListener)}: tcp://{endPoint}";
        }
    }
}