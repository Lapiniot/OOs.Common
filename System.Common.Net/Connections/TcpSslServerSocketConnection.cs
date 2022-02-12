using System.Net.Security;
using System.Net.Sockets;
using System.Net.Connections.Exceptions;
using static System.Net.Sockets.SocketError;

namespace System.Net.Connections;

public sealed class TcpSslServerSocketConnection : NetworkConnection
{
    private readonly SslStream sslStream;
    private readonly Socket socket;
    private readonly SslServerAuthenticationOptions options;
    private readonly EndPoint remoteEndPoint;

    public TcpSslServerSocketConnection(Socket acceptedSocket, SslServerAuthenticationOptions options)
    {
        socket = acceptedSocket;
        this.options = options;
        remoteEndPoint = socket.RemoteEndPoint;

        var stream = new NetworkStream(acceptedSocket, FileAccess.ReadWrite, true);

        try
        {
            sslStream = new SslStream(stream, false);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    public override async ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        try
        {
            await sslStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        catch(SocketException se) when(se.SocketErrorCode is ConnectionAborted or ConnectionReset or Shutdown)
        {
            await StopActivityAsync().ConfigureAwait(false);
            throw new ConnectionClosedException(se);
        }
    }

    public override async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        try
        {
            return await sslStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        catch(SocketException se) when(se.SocketErrorCode is ConnectionAborted or ConnectionReset or Shutdown)
        {
            await StopActivityAsync().ConfigureAwait(false);
            throw new ConnectionClosedException(se);
        }
    }

    protected override Task StartingAsync(CancellationToken cancellationToken)
    {
        return sslStream.AuthenticateAsServerAsync(options, cancellationToken);
    }

    protected override async Task StoppingAsync()
    {
        await socket.DisconnectAsync(false).ConfigureAwait(false);
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            await base.DisposeAsync().ConfigureAwait(true);
        }
        finally
        {
            await sslStream.DisposeAsync().ConfigureAwait(false);
        }
    }

    public override string ToString()
    {
        return $"{Id}-{nameof(TcpSslServerSocketConnection)}-{remoteEndPoint}";
    }
}