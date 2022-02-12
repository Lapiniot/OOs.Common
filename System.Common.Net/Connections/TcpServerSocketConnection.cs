using System.Net.Connections.Exceptions;
using System.Net.Sockets;
using static System.Net.Sockets.SocketFlags;
using static System.Net.Sockets.SocketError;

namespace System.Net.Connections;

public sealed class TcpServerSocketConnection : INetworkConnection
{
    private readonly Socket socket;
    private readonly EndPoint remoteEndPoint;
    private int disposed;

    public TcpServerSocketConnection(Socket acceptedSocket)
    {
        ArgumentNullException.ThrowIfNull(acceptedSocket);
        socket = acceptedSocket;
        remoteEndPoint = socket.RemoteEndPoint;
        Id = Base32.ToBase32String(CorrelationIdGenerator.GetNext());
    }

    public bool IsConnected => socket.Connected;
    public Socket Socket => socket;
    public string Id { get; }

    public async ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        try
        {
            await socket.SendAsync(buffer, None, cancellationToken).ConfigureAwait(false);
        }
        catch(SocketException se) when(se.SocketErrorCode is ConnectionAborted or ConnectionReset or Shutdown)
        {
            throw new ConnectionClosedException(se);
        }
    }

    public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        try
        {
            return await socket.ReceiveAsync(buffer, None, cancellationToken).ConfigureAwait(false);
        }
        catch(SocketException se) when(se.SocketErrorCode is ConnectionAborted or ConnectionReset or Shutdown)
        {
            throw new ConnectionClosedException(se);
        }
    }

    public ValueTask DisposeAsync()
    {
        if(Interlocked.CompareExchange(ref disposed, 1, 0) != 0)
        {
            return ValueTask.CompletedTask;
        }

        using(socket)
        {
            if(socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
            }
        }

        return ValueTask.CompletedTask;
    }

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task DisconnectAsync()
    {
        await socket.DisconnectAsync(false).ConfigureAwait(false);
    }

    public override string ToString()
    {
        return $"{Id}-{nameof(TcpServerSocketConnection)}-{remoteEndPoint}";
    }
}