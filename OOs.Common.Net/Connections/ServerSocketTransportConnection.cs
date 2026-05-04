using System.IO.Pipelines;
using System.Net.Sockets;

namespace OOs.Net.Connections;

public abstract class ServerSocketTransportConnection(Socket acceptedSocket,
    PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) :
    SocketTransportConnection(acceptedSocket, inputPipeOptions, outputPipeOptions)
{
    protected override ValueTask OnStartingAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    protected override ValueTask OnStoppingAsync()
    {
        Shutdown();
        return Socket.DisconnectAsync(reuseSocket: false);
    }
}