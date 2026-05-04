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

    public override void Abort() => Shutdown();

    protected override ValueTask<int> ReceiveAsync(Memory<byte> buffer) => socket.ReceiveAsync(buffer, None);

    protected override async ValueTask SendAsync(ReadOnlyMemory<byte> buffer) =>
        await socket.SendAsync(buffer, None).ConfigureAwait(false);

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        using (socket)
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }

    protected void Shutdown()
    {
        try
        {
            if (socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
            }
        }
        catch (SocketException)
        {
            // Highly anticipated on Windows when socket is not connected 
            // or remote peer closes connection at the moment of Shutdown() is called, etc.
        }
    }
}