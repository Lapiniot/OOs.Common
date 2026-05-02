using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using static System.Net.Sockets.SocketFlags;

namespace OOs.Net.Connections;

public abstract class SocketTransportConnection : TransportConnectionPipeAdapter
{
    private readonly Socket socket;

    protected SocketTransportConnection(Socket socket,
        PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) :
        base(inputPipeOptions, outputPipeOptions)
    {
        ArgumentNullException.ThrowIfNull(socket);
        this.socket = socket;
    }

    public sealed override string Id { get; } = Base32.ToBase32String(CorrelationIdGenerator.GetNext());

    public sealed override EndPoint? LocalEndPoint => socket.LocalEndPoint;

    public sealed override EndPoint? RemoteEndPoint => socket.RemoteEndPoint;

    protected Socket Socket => socket;

    public override void Abort()
    {
        if (socket.Connected)
        {
            socket.Shutdown(SocketShutdown.Both);
        }
    }

    protected override ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken) =>
        socket.ReceiveAsync(buffer, None, cancellationToken);

    protected override async ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) =>
        await socket.SendAsync(buffer, None, cancellationToken).ConfigureAwait(false);

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        using (socket)
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}