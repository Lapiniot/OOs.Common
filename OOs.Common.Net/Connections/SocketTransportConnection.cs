using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using static System.Net.Sockets.SocketError;
using static System.Net.Sockets.SocketFlags;

#nullable enable

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

    protected override async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        try
        {
            return await socket.ReceiveAsync(buffer, None, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException se) when (se.SocketErrorCode is ConnectionAborted or ConnectionReset or Shutdown)
        {
            ThrowHelper.ThrowConnectionClosed(se);
            return 0;
        }
    }

    protected override async ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        try
        {
            await socket.SendAsync(buffer, None, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException se) when (se.SocketErrorCode is ConnectionAborted or ConnectionReset or Shutdown)
        {
            ThrowHelper.ThrowConnectionClosed(se);
        }
    }

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        using (socket)
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}