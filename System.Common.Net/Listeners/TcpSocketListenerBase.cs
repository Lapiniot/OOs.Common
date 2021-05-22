using System.Collections.Generic;
using System.Net.Connections;
using System.Net.Sockets;
using System.Threading;

namespace System.Net.Listeners
{
    public abstract class TcpSocketListenerBase : IAsyncEnumerable<INetworkConnection>
    {
        private readonly int backlog;
        private readonly Action<Socket> configureListening;
        private readonly Action<Socket> configureAccepted;
        private readonly IPEndPoint endPoint;

        protected TcpSocketListenerBase(IPEndPoint endPoint, int backlog = 100,
            Action<Socket> configureListening = null,
            Action<Socket> configureAccepted = null)
        {
            this.endPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            this.backlog = backlog;
            this.configureListening = configureListening;
            this.configureAccepted = configureAccepted;
        }

        protected IPEndPoint EndPoint => endPoint;

        #region Implementation of IAsyncEnumerable<INetworkConnection>

        public async IAsyncEnumerator<INetworkConnection> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            using var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            configureListening?.Invoke(socket);
            socket.Bind(endPoint);
            socket.Listen(backlog);

            while(!cancellationToken.IsCancellationRequested)
            {
                Socket acceptedSocket = null;

                try
                {
                    acceptedSocket = await socket.AcceptAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
                    configureAccepted?.Invoke(acceptedSocket);
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

                yield return CreateConnection(acceptedSocket);
            }
        }

        #endregion

        protected abstract INetworkConnection CreateConnection(Socket acceptedSocket);
    }
}