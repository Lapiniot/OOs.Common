using System.IO.Pipelines;
using System.Net.Sockets;

#nullable enable

namespace OOs.Net.Connections;

public sealed class ServerUnixDomainSocketTransportConnection(Socket socket,
    PipeOptions? inputPipeOptions = null,
    PipeOptions? outputPipeOptions = null) :
    ServerSocketTransportConnection(socket, inputPipeOptions, outputPipeOptions)
{
    public override string ToString() => $"{Id}-UD";
}