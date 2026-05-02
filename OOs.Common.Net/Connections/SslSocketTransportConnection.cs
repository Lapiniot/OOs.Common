using System.IO.Pipelines;
using System.Net.Security;
using System.Net.Sockets;

namespace OOs.Net.Connections;

public abstract class SslSocketTransportConnection(Socket socket,
    PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) :
    SocketTransportConnection(socket, inputPipeOptions, outputPipeOptions)
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

    protected sealed override ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken) =>
        stream!.ReadAsync(buffer, cancellationToken);

    protected sealed override ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) =>
        stream!.WriteAsync(buffer, cancellationToken);

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        await using (stream)
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}