using System.Net.Sockets;
using static System.Net.Sockets.SocketFlags;
using static System.Net.Sockets.SocketShutdown;
using static System.Threading.Tasks.Task;

namespace System.Net.Connections;

public sealed class TcpSocketServerConnection : INetworkConnection
{
    private readonly Socket socket;
    private int disposed;

    public TcpSocketServerConnection(Socket acceptedSocket)
    {
        ArgumentNullException.ThrowIfNull(acceptedSocket);
        socket = acceptedSocket;
        Id = Base32.ToBase32String(CorrelationIdGenerator.GetNext());
    }

    public async ValueTask SendAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var vt = socket.SendAsync(buffer, None, cancellationToken);
        if(!vt.IsCompletedSuccessfully)
        {
            await vt.ConfigureAwait(false);
        }
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
    public string Id { get; }

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
        return $"{Id}-{nameof(TcpSocketServerConnection)}-{socket?.RemoteEndPoint}";
    }
}