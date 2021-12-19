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
            var vt = sslStream.WriteAsync(buffer, cancellationToken);
            if(!vt.IsCompletedSuccessfully)
            {
                await vt.ConfigureAwait(false);
            }
        }
        catch(SocketException se) when(se.SocketErrorCode is ConnectionAborted or ConnectionReset)
        {
            await StopActivityAsync().ConfigureAwait(false);
            throw new ConnectionAbortedException(se);
        }
    }

    public override async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        try
        {
            var vt = sslStream.ReadAsync(buffer, cancellationToken);
            return vt.IsCompletedSuccessfully ? vt.Result : await vt.ConfigureAwait(false);
        }
        catch(SocketException se) when(se.SocketErrorCode is ConnectionAborted or ConnectionReset)
        {
            await StopActivityAsync().ConfigureAwait(false);
            throw new ConnectionAbortedException(se);
        }
    }

    protected override Task StartingAsync(CancellationToken cancellationToken)
    {
        return sslStream.AuthenticateAsServerAsync(options, cancellationToken);
    }

    protected override async Task StoppingAsync()
    {
        var vt = socket.DisconnectAsync(false);
        if(!vt.IsCompletedSuccessfully)
        {
            await vt.ConfigureAwait(false);
        }
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