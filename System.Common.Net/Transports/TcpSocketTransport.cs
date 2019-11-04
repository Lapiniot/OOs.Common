using System.Net.Sockets;
using System.Net.Transports.Exceptions;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Dns;
using static System.Net.Properties.Strings;
using static System.Net.Sockets.ProtocolType;
using static System.Net.Sockets.SocketShutdown;
using static System.Net.Sockets.SocketType;
using static System.Net.Sockets.SocketError;
using static System.Net.Sockets.SocketFlags;
using static System.Threading.Tasks.Task;

namespace System.Net.Transports
{
    public class TcpSocketTransport : NetworkTransport
    {
        private readonly string hostNameOrAddress;
        private readonly int port;
        private Socket socket;

        public TcpSocketTransport(IPEndPoint ipEndPoint)
        {
            RemoteEndPoint = ipEndPoint ?? throw new ArgumentNullException(nameof(ipEndPoint));
        }

        public TcpSocketTransport(string hostNameOrAddress, int port)
        {
            if(hostNameOrAddress == null) throw new ArgumentNullException(nameof(hostNameOrAddress));
            if(hostNameOrAddress == "") throw new ArgumentException(NotEmptyExpected, nameof(hostNameOrAddress));
            this.hostNameOrAddress = hostNameOrAddress;
            this.port = port;
        }

        public IPEndPoint RemoteEndPoint { get; private set; }

        public override async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            CheckConnected();
            try
            {
                var vt = socket.ReceiveAsync(buffer, None, cancellationToken);
                return vt.IsCompletedSuccessfully ? vt.Result : await vt.AsTask().ConfigureAwait(false);
            }
            catch(SocketException se) when(
                se.SocketErrorCode == ConnectionAborted ||
                se.SocketErrorCode == ConnectionReset)
            {
                await DisconnectAsync().ConfigureAwait(false);
                throw new ConnectionAbortedException(se);
            }
        }

        public override async ValueTask<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            CheckConnected();
            try
            {
                var vt = socket.SendAsync(buffer, None, cancellationToken);
                return vt.IsCompletedSuccessfully ? vt.Result : await vt.AsTask().ConfigureAwait(false);
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
                if(RemoteEndPoint == null)
                {
                    var addresses = await GetHostAddressesAsync(hostNameOrAddress).ConfigureAwait(false);
                    RemoteEndPoint = new IPEndPoint(addresses[0], port);
                }

                socket = new Socket(RemoteEndPoint.AddressFamily, Stream, Tcp);

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

        public override string ToString()
        {
            return $"{nameof(TcpSocketTransport)}: {socket?.RemoteEndPoint?.ToString() ?? "Not connected"}";
        }
    }
}