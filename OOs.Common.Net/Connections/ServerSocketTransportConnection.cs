using System.IO.Pipelines;
using System.Net.Sockets;

#nullable enable

namespace OOs.Net.Connections;

public abstract class ServerSocketTransportConnection(Socket acceptedSocket,
    PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) :
    SocketTransportConnection(acceptedSocket, inputPipeOptions, outputPipeOptions)
{
    protected override ValueTask OnStartingAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    protected override ValueTask OnStoppingAsync()
    {
        Socket.Shutdown(SocketShutdown.Both);
        return Socket.DisconnectAsync(reuseSocket: false);
    }
}