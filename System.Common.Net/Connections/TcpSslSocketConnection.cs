using System.Net.Connections.Exceptions;
using System.Net.Security;
using System.Net.Sockets;
using static System.Net.Sockets.SocketError;

namespace System.Net.Connections;

public abstract class TcpSslSocketConnection : TcpSocketConnection
{
    private SslStream sslStream;

    protected TcpSslSocketConnection()
    { }

    protected TcpSslSocketConnection(IPEndPoint remoteEndPoint) : base(remoteEndPoint)
    { }

    protected TcpSslSocketConnection(Socket acceptedSocket) : base(acceptedSocket)
    { }

    protected SslStream SslStream { get => sslStream; set => sslStream = value; }

    public override async ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        CheckState(true);

        try
        {
            await sslStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException se) when (se.SocketErrorCode is ConnectionAborted or ConnectionReset or Shutdown)
        {
            await StopActivityAsync().ConfigureAwait(false);
            throw new ConnectionClosedException(se);
        }
    }

    public override async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        CheckState(true);

        try
        {
            return await sslStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException se) when (se.SocketErrorCode is ConnectionAborted or ConnectionReset or Shutdown)
        {
            await StopActivityAsync().ConfigureAwait(false);
            throw new ConnectionClosedException(se);
        }
    }

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        try
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            if (SslStream is not null)
            {
                await SslStream.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    public override string ToString() => $"{Id}-TCP.SSL-{RemoteEndPoint}";

    protected static SslStream CreateSslStream(Socket socket)
    {
        var networkStream = new NetworkStream(socket, FileAccess.ReadWrite, true);

        try
        {
            return new(networkStream, false);
        }
        catch
        {
            using (networkStream)
            {
                throw;
            }
        }
    }
}