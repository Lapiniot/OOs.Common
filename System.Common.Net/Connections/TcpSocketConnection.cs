using System.Net.Connections.Exceptions;
using System.Net.Sockets;
using static System.Net.Sockets.SocketFlags;
using static System.Net.Sockets.SocketError;

namespace System.Net.Connections;

public abstract class TcpSocketConnection : NetworkConnection
{
    private Socket socket;
    private IPEndPoint remoteEndPoint;
    private int disposed;

    protected Socket Socket { get => socket; set => socket = value; }
    public IPEndPoint RemoteEndPoint { get => remoteEndPoint; protected set => remoteEndPoint = value; }

    public override async ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        try
        {
            await socket.SendAsync(buffer, None, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException se) when (se.SocketErrorCode is ConnectionAborted or ConnectionReset or Shutdown)
        {
            await StopActivityAsync().ConfigureAwait(false);
            throw new ConnectionClosedException(se);
        }
    }

    public override async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        try
        {
            return await socket.ReceiveAsync(buffer, None, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException se) when (se.SocketErrorCode is ConnectionAborted or ConnectionReset or Shutdown)
        {
            await StopActivityAsync().ConfigureAwait(false);
            throw new ConnectionClosedException(se);
        }
    }

    protected override async Task StoppingAsync()
    {
        socket.Shutdown(SocketShutdown.Both);
        await socket.DisconnectAsync(false).ConfigureAwait(false);
    }

    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref disposed, 1, 0) != 0) return;

        GC.SuppressFinalize(this);

        using (socket)
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }

    public override string ToString() => $"{Id}-TCP-{remoteEndPoint}";
}