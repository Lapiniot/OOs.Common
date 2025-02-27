using System.IO.Pipelines;
using System.Net.Security;
using System.Net.Sockets;
using static System.Net.Sockets.SocketError;

#nullable enable

namespace OOs.Net.Connections;

public abstract class SslSocketTransportConnection(Socket socket,
    PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) :
    SocketTransportConnection(socket, inputPipeOptions, outputPipeOptions)
{
    private SslStream? stream;

    protected SslStream? Stream => stream;

    protected override ValueTask OnStartingAsync()
    {
        var networkStream = new NetworkStream(Socket, FileAccess.ReadWrite, ownsSocket: false);
        try
        {
            stream = new SslStream(networkStream, leaveInnerStreamOpen: false);
        }
        catch
        {
            using (networkStream) throw;
        }

        return ValueTask.CompletedTask;
    }

    protected override async ValueTask OnStoppingAsync()
    {
        if (stream is not null)
        {
            using (stream)
            {
                await stream.ShutdownAsync().ConfigureAwait(false);
            }
        }
    }

    protected sealed override async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        try
        {
            return await stream!.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException se) when (se.SocketErrorCode is ConnectionAborted
            or ConnectionReset or Shutdown)
        {
            ThrowHelper.ThrowConnectionClosed(se);
            return 0;
        }
    }

    protected sealed override async ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        try
        {
            await stream!.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException se) when (se.SocketErrorCode is ConnectionAborted
            or ConnectionReset or Shutdown)
        {
            ThrowHelper.ThrowConnectionClosed(se);
        }
    }

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        await using (stream)
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}