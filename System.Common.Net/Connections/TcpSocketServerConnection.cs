using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Sockets.SocketFlags;
using static System.Net.Sockets.SocketShutdown;
using static System.Threading.Tasks.Task;

namespace System.Net.Connections
{
    public sealed class TcpSocketServerConnection : INetworkConnection
    {
        private readonly Socket socket;
        private int disposed;

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
            if(Interlocked.CompareExchange(ref disposed, 1, 0) != 0) return;

            using(socket)
            {
                await DisconnectAsync().ConfigureAwait(false);
            }
        }

        public bool IsConnected => socket.Connected;
        public Socket Socket => socket;

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