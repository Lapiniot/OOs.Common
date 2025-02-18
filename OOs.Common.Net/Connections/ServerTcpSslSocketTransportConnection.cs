using System.IO.Pipelines;
using System.Net.Security;
using System.Net.Sockets;

#nullable enable

namespace OOs.Net.Connections;

public sealed class ServerTcpSslSocketTransportConnection(Socket acceptedSocket,
    SslServerAuthenticationOptions options,
    PipeOptions? inputPipeOptions = null,
    PipeOptions? outputPipeOptions = null) :
    ServerSocketTransportConnection(acceptedSocket, inputPipeOptions, outputPipeOptions)
{
    private SslStream? stream;

    public override string ToString() => $"{Id}-TCP.SSL ({RemoteEndPoint})";

    protected override async ValueTask OnStartingAsync(CancellationToken cancellationToken)
    {
        var innerStream = new NetworkStream(Socket, FileAccess.ReadWrite, true);
        try
        {
            stream = new SslStream(innerStream, false);
            await stream.AuthenticateAsServerAsync(options, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            using (innerStream)
            using (stream)
            {
                throw;
            }
        }

        await base.OnStartingAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override async ValueTask OnStoppingAsync()
    {
        await using (stream)
        {
            await base.OnStoppingAsync().ConfigureAwait(false);
        }
    }

    protected override async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        try
        {
            return await stream!.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException se) when (se.SocketErrorCode is SocketError.ConnectionAborted
            or SocketError.ConnectionReset or SocketError.Shutdown)
        {
            ThrowHelper.ThrowConnectionClosed(se);
            return 0;
        }
    }

    protected override async ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        try
        {
            await stream!.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException se) when (se.SocketErrorCode is SocketError.ConnectionAborted
            or SocketError.ConnectionReset or SocketError.Shutdown)
        {
            ThrowHelper.ThrowConnectionClosed(se);
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await using (stream)
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}