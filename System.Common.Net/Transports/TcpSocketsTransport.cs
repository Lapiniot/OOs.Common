using System.Net.Sockets;
using System.Net.Transports.Exceptions;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Sockets.AddressFamily;
using static System.Net.Sockets.ProtocolType;
using static System.Net.Sockets.SocketShutdown;
using static System.Net.Sockets.SocketType;
using static System.Net.Sockets.SocketError;
using static System.Threading.Tasks.Task;

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

        public override async Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            CheckConnected();
            try
            {
                return await socket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
            }
            catch(SocketException se) when(
                se.SocketErrorCode == ConnectionAborted ||
                se.SocketErrorCode == ConnectionReset)
            {
                await DisconnectAsync().ConfigureAwait(false);

                throw new ConnectionAbortedException(se);
            }
        }

        public override async Task<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            CheckConnected();
            try
            {
                return await socket.SendAsync(buffer, cancellationToken).ConfigureAwait(false);
            }
            catch(SocketException se) when(
                se.SocketErrorCode == ConnectionAborted ||
                se.SocketErrorCode == ConnectionReset)
            {
                await DisconnectAsync().ConfigureAwait(false);

                throw new ConnectionAbortedException(se);
            }
        }

        protected override Task OnDisconnectAsync()
        {
            socket.Shutdown(Both);

            socket.Close();

            socket = null;

            return CompletedTask;
        }

        protected override Task OnConnectAsync(CancellationToken cancellationToken)
        {
            socket = new Socket(InterNetwork, Stream, Tcp);

            return socket.ConnectAsync(RemoteEndPoint);
        }

        protected override Task OnConnectedAsync(CancellationToken cancellationToken)
        {
            return CompletedTask;
        }
    }
}