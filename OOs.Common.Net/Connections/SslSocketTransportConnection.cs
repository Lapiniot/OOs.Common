using System.Buffers;
using System.IO.Pipelines;
using System.Net.Security;
using System.Net.Sockets;

namespace OOs.Net.Connections;

public abstract class SslSocketTransportConnection(Socket socket,
    PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) :
    SocketTransportConnectionBase(socket, inputPipeOptions, outputPipeOptions)
{
    private SslStream? stream;

    protected SslStream? Stream => stream;

    protected override async ValueTask ShutdownAsync(ShutdownDirection direction)
    {
        if (stream is null)
        {
            return;
        }

        if (direction is ShutdownDirection.Send or ShutdownDirection.Both)
        {
            await stream.ShutdownAsync().ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }

        await base.ShutdownAsync(direction).ConfigureAwait(false);
    }

    protected override async ValueTask OnStartingAsync(CancellationToken cancellationToken)
    {
        var networkStream = new NetworkStream(Socket, FileAccess.ReadWrite, ownsSocket: false);
        try
        {
            stream = new(networkStream, leaveInnerStreamOpen: false);
        }
        catch
        {
            await networkStream.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    protected override async ValueTask OnStoppingAsync()
    {
        if (stream is null)
        {
            return;
        }

        using (stream)
        {
            await stream.ShutdownAsync().ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            Socket.Shutdown(SocketShutdown.Both);
        }
    }

    protected sealed override ValueTask<int> ReceiveAsync(Memory<byte> buffer) => stream!.ReadAsync(buffer);

    protected sealed override ValueTask SendAsync(ref readonly ReadOnlySequence<byte> buffer)
    {
        return buffer.IsSingleSegment ? stream!.WriteAsync(buffer.First) : SendAsync(buffer);
    }

    private async ValueTask SendAsync(ReadOnlySequence<byte> buffer)
    {
        var position = buffer.Start;
        while (buffer.TryGet(ref position, out var memory))
        {
            await stream!.WriteAsync(memory).ConfigureAwait(false);
        }
    }

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        await using (stream)
        {
            await base.DisposeAsync().AsTask().ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }
}