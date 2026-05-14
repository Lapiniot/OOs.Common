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

    protected override ValueTask OnStartingAsync(CancellationToken cancellationToken)
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
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}