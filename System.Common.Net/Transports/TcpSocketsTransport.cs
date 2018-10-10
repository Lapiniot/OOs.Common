using System.Net.Sockets;
using System.Net.Transports.Exceptions;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Dns;
using static System.Net.Sockets.AddressFamily;
using static System.Net.Sockets.ProtocolType;
using static System.Net.Sockets.SocketShutdown;
using static System.Net.Sockets.SocketType;
using static System.Net.Sockets.SocketError;
using static System.String;
using static System.Threading.Tasks.Task;

namespace System.Net.Transports
{
    public class TcpSocketsTransport : NetworkTransport
    {
        private readonly string hostNameOrAddress;
        private readonly int port;
        private Socket socket;

        public TcpSocketsTransport(IPEndPoint ipEndPoint)
        {
            RemoteEndPoint = ipEndPoint ?? throw new ArgumentNullException(nameof(ipEndPoint));
        }

        public TcpSocketsTransport(string hostNameOrAddress, int port)
        {
            if(hostNameOrAddress == null) throw new ArgumentNullException(nameof(hostNameOrAddress));
            if(hostNameOrAddress == "") throw new ArgumentException("Value cannot be empty.", nameof(hostNameOrAddress));
            this.hostNameOrAddress = hostNameOrAddress;
            this.port = port;
        }

        public IPEndPoint RemoteEndPoint { get; private set; }

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

        protected override async Task OnConnectAsync(CancellationToken cancellationToken)
        {
            try
            {
                socket = new Socket(InterNetwork, Stream, Tcp);

                if(RemoteEndPoint == null && !IsNullOrEmpty(hostNameOrAddress))
                {
                    var addresses = await GetHostAddressesAsync(hostNameOrAddress).ConfigureAwait(false);
                    RemoteEndPoint = new IPEndPoint(addresses[0], port);
                }

                await socket.ConnectAsync(RemoteEndPoint).ConfigureAwait(false);
            }
            catch(SocketException se) when(se.SocketErrorCode == HostNotFound)
            {
                throw new HostNotFoundException(se);
            }
            catch(SocketException se)
            {
                throw new ServerUnavailableException(se);
            }
        }
    }
}