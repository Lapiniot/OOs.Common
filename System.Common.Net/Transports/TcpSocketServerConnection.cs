using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Sockets.SocketFlags;
using static System.Net.Sockets.SocketShutdown;
using static System.Threading.Tasks.Task;

namespace System.Net.Transports
{
    public class TcpSocketServerConnection : INetworkConnection
    {
        private readonly Socket socket;

        public TcpSocketServerConnection(Socket acceptedSocket)
        {
            socket = acceptedSocket ?? throw new ArgumentNullException(nameof(acceptedSocket));
        }

        public ValueTask<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            return socket.SendAsync(buffer, None, cancellationToken);
        }

        public ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            return socket.ReceiveAsync(buffer, None, cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            using(socket)
            {
                await DisconnectAsync().ConfigureAwait(false);
            }
        }

        public bool IsConnected => socket.Connected;

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            return CompletedTask;
        }

        public Task DisconnectAsync()
        {
            socket.Shutdown(Both);
            socket.Close();
            return CompletedTask;
        }

        public override string ToString()
        {
            return $"{nameof(TcpSocketServerConnection)}: {socket?.RemoteEndPoint}";
        }
    }
}