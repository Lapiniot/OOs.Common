using System.IO.Pipelines;
using System.Net.Sockets;

namespace OOs.Net.Connections;

public sealed class ServerTcpSocketTransportConnection(Socket acceptedSocket,
    PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) :
    ServerSocketTransportConnection(acceptedSocket, inputPipeOptions, outputPipeOptions)
{
    public override string ToString() => $"{Id}-TCP ({RemoteEndPoint})";
}