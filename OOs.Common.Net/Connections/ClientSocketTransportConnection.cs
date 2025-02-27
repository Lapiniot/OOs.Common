using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

#nullable enable

namespace OOs.Net.Connections;

public abstract class ClientSocketTransportConnection : SocketTransportConnection
{
    private readonly EndPoint remoteEndPoint;

    protected ClientSocketTransportConnection(Socket socket, EndPoint remoteEndPoint,
        PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) :
        base(socket, inputPipeOptions, outputPipeOptions)
    {
        ArgumentNullException.ThrowIfNull(remoteEndPoint);
        this.remoteEndPoint = remoteEndPoint;
    }

    protected override async ValueTask OnStartingAsync()
    {
        try
        {
            await Socket.ConnectAsync(remoteEndPoint).ConfigureAwait(false);
        }
        catch (SocketException se) when (se.SocketErrorCode == SocketError.HostNotFound)
        {
            ThrowHelper.ThrowHostNotFound(se);
        }
        catch (SocketException se)
        {
            ThrowHelper.ThrowServerUnavailable(se);
        }
    }

    protected override ValueTask OnStoppingAsync()
    {
        Socket.Shutdown(SocketShutdown.Both);
        return Socket.DisconnectAsync(reuseSocket: true);
    }
}