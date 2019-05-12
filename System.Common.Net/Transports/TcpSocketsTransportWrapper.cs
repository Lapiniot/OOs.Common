using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Sockets.SocketFlags;
using static System.Net.Sockets.SocketShutdown;
using static System.Threading.Tasks.Task;

namespace System.Net.Transports
{
    public class TcpSocketsTransportWrapper : INetworkTransport
    {
        private readonly Socket socket;

        public TcpSocketsTransportWrapper(Socket socket)
        {
            this.socket = socket ?? throw new ArgumentNullException(nameof(socket));
        }

        public ValueTask<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            return socket.SendAsync(buffer, None, cancellationToken);
        }

        public ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            return socket.ReceiveAsync(buffer, None, cancellationToken);
        }

        public void Dispose()
        {
            socket.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            socket.Dispose();
            return default;
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
            return $"{nameof(TcpSocketsTransportWrapper)}: {socket?.RemoteEndPoint}";
        }
    }
}