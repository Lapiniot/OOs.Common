using System.Collections.Generic;
using System.Net.Sockets;
using System.Net.Transports;
using System.Threading;
using static System.Net.Sockets.ProtocolType;
using static System.Net.Sockets.SocketType;

namespace System.Net.Listeners
{
    public sealed class TcpSocketListener : AsyncAsyncConnectionListener
    {
        private readonly int backlog;
        private readonly IPEndPoint ipEndPoint;

        public TcpSocketListener(IPEndPoint ipEndPoint, int backlog = 100)
        {
            this.ipEndPoint = ipEndPoint;
            this.backlog = backlog;
        }

        protected override async IAsyncEnumerable<INetworkTransport> GetAsyncEnumerable(CancellationToken cancellationToken)
        {
            using var socket = new Socket(ipEndPoint.AddressFamily, Stream, Tcp);
            using var tokenRegistration = cancellationToken.Register(socket.Close);

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
    }
}