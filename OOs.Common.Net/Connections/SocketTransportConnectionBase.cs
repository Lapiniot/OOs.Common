using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

namespace OOs.Net.Connections;

/// <summary>
/// Provides base abstract adapting implementation for a transport connection whose 
/// underlaying adaptee is <see cref="System.Net.Sockets.Socket"/> instance.
/// </summary>
public abstract class SocketTransportConnectionBase : TransportConnectionPipeAdapter
{
    private readonly Socket socket;

    protected SocketTransportConnectionBase(Socket socket,
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

    protected void Shutdown(ShutdownDirection direction)
    {
        try
        {
            if (socket.Connected)
            {
                socket.Shutdown(how: direction switch
                {
                    ShutdownDirection.Send => SocketShutdown.Send,
                    ShutdownDirection.Receive => SocketShutdown.Receive,
                    ShutdownDirection.Both => SocketShutdown.Both,
                    _ => ThrowInvalidShutdownDirection<SocketShutdown>(direction)
                });
            }
        }
        catch (SocketException)
        {
            // Highly anticipated on Windows when socket is not connected 
            // or remote peer closes connection at the moment of Shutdown() is called, etc.
        }
    }

    public sealed override void Abort()
    {
        Shutdown(ShutdownDirection.Both);
        base.Abort();
    }

    protected override ValueTask ShutdownAsync(ShutdownDirection direction)
    {
        Shutdown(direction);
        return default;
    }

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        using (socket)
        {
            await base.DisposeAsync().AsTask().ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }
}