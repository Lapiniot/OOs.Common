using System.Net.Connections.Exceptions;
using System.Net.Sockets;
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

namespace System.Net.Connections
{
    public class TcpSocketClientConnection : NetworkConnection
    {
        private readonly string hostNameOrAddress;
        private readonly int port;
        private Socket socket;

        public TcpSocketClientConnection(IPEndPoint ipEndPoint)
        {
            RemoteEndPoint = ipEndPoint ?? throw new ArgumentNullException(nameof(ipEndPoint));
        }

        public TcpSocketClientConnection(string hostNameOrAddress, int port)
        {
            if(hostNameOrAddress == null) throw new ArgumentNullException(nameof(hostNameOrAddress));
            if(hostNameOrAddress.Length == 0) throw new ArgumentException(NotEmptyExpected, nameof(hostNameOrAddress));
            this.hostNameOrAddress = hostNameOrAddress;
            this.port = port;
        }

        public IPEndPoint RemoteEndPoint { get; private set; }

        public override async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            CheckState(true);
            try
            {
                var vt = socket.ReceiveAsync(buffer, None, cancellationToken);
                return vt.IsCompletedSuccessfully ? vt.Result : await vt.ConfigureAwait(false);
            }
            catch(SocketException se) when(
                se.SocketErrorCode == ConnectionAborted ||
                se.SocketErrorCode == ConnectionReset)
            {
                await StopActivityAsync().ConfigureAwait(false);
                throw new ConnectionAbortedException(se);
            }
        }

        public override async ValueTask<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            CheckState(true);
            try
            {
                var vt = socket.SendAsync(buffer, None, cancellationToken);
                return vt.IsCompletedSuccessfully ? vt.Result : await vt.ConfigureAwait(false);
            }
            catch(SocketException se) when(
                se.SocketErrorCode == ConnectionAborted ||
                se.SocketErrorCode == ConnectionReset)
            {
                await StopActivityAsync().ConfigureAwait(false);
                throw new ConnectionAbortedException(se);
            }
        }

        protected override Task StoppingAsync()
        {
            socket.Shutdown(Both);

            socket.Close();

            socket = null;

            return CompletedTask;
        }

        protected override async Task StartingAsync(object state, CancellationToken cancellationToken)
        {
            try
            {
                if(RemoteEndPoint == null)
                {
                    var addresses = await GetHostAddressesAsync(hostNameOrAddress, cancellationToken).ConfigureAwait(false);
                    RemoteEndPoint = new IPEndPoint(addresses[0], port);
                }

                socket = new Socket(RemoteEndPoint.AddressFamily, Stream, Tcp);

                await socket.ConnectAsync(RemoteEndPoint, cancellationToken).ConfigureAwait(false);
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
            return $"{nameof(TcpSocketClientConnection)}: {socket?.RemoteEndPoint?.ToString() ?? "Not connected"}";
        }
    }
}