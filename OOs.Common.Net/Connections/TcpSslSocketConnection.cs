using System.Net;
using System.Net.Security;
using System.Net.Sockets;

namespace OOs.Net.Connections;

public abstract class TcpSslSocketConnection : SocketConnection
{
    private SslStream sslStream;

    protected TcpSslSocketConnection(bool reuseSocket) : base(reuseSocket)
    { }

    protected TcpSslSocketConnection(IPEndPoint remoteEndPoint, bool reuseSocket) :
        base(remoteEndPoint, ProtocolType.Tcp, reuseSocket)
    { }

    protected TcpSslSocketConnection(Socket socket, bool reuseSocket) :
        base(socket, reuseSocket)
    { }

    protected SslStream SslStream { get => sslStream; set => sslStream = value; }

    public override async ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        CheckState();

        try
        {
            await sslStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException se) when (se.SocketErrorCode is SocketError.ConnectionAborted or SocketError.ConnectionReset or SocketError.Shutdown)
        {
            ThrowConnectionClosed(se);
        }
    }

    public override async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        CheckState();

        try
        {
            return await sslStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException se) when (se.SocketErrorCode is SocketError.ConnectionAborted or SocketError.ConnectionReset or SocketError.Shutdown)
        {
            ThrowConnectionClosed(se);
            return 0;
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
                await SslStream.DisposeAsync().ConfigureAwait(false);
        }
    }

    public override string ToString() => $"{Id}-TCP.SSL ({RemoteEndPoint})";

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