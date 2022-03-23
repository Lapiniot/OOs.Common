using System.Net.Connections.Exceptions;
using System.Net.Sockets;
using static System.Net.Sockets.SocketFlags;
using static System.Net.Sockets.SocketError;

namespace System.Net.Connections;

public sealed class TcpServerSocketConnection : NetworkConnection
{
    private readonly EndPoint remoteEndPoint;
    private readonly Socket socket;
    private int disposed;

    public TcpServerSocketConnection(Socket acceptedSocket)
    {
        ArgumentNullException.ThrowIfNull(acceptedSocket);
        socket = acceptedSocket;
        remoteEndPoint = socket.RemoteEndPoint;
    }

    public override string ToString() => $"{Id}-{nameof(TcpServerSocketConnection)}-{remoteEndPoint}";

    public override async ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        try
        {
            await socket.SendAsync(buffer, None, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException se) when (se.SocketErrorCode is ConnectionAborted or ConnectionReset or Shutdown)
        {
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
            throw new ConnectionClosedException(se);
        }
    }

    public override ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref disposed, 1, 0) != 0)
        {
            return ValueTask.CompletedTask;
        }

        using (socket)
        {
            if (socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
            }
        }

        return base.DisposeAsync();
    }

    protected override Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    protected override async Task StoppingAsync() => await socket.DisconnectAsync(false).ConfigureAwait(false);
}