using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

#nullable enable

namespace OOs.Net.Connections;

public sealed class ClientTcpSocketTransportConnection(Socket socket, EndPoint remoteEndPoint,
    PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) :
    ClientSocketTransportConnection(socket, remoteEndPoint, inputPipeOptions, outputPipeOptions)
{
    public override string ToString() => $"{Id}-TCP ({RemoteEndPoint?.ToString() ?? "Not connected"})";

    public static ClientTcpSocketTransportConnection Create(IPEndPoint remoteEndPoint,
        PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) =>
        CreateInternal(remoteEndPoint, inputPipeOptions, outputPipeOptions);

    public static ClientTcpSocketTransportConnection Create(DnsEndPoint remoteEndPoint,
        PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) =>
        CreateInternal(remoteEndPoint, inputPipeOptions, outputPipeOptions);

    private static ClientTcpSocketTransportConnection CreateInternal(EndPoint remoteEndPoint,
        PipeOptions? inputPipeOptions, PipeOptions? outputPipeOptions)
    {
        ArgumentNullException.ThrowIfNull(remoteEndPoint);

#pragma warning disable CA2000 // Dispose objects before losing scope
        var socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
#pragma warning restore CA2000 // Dispose objects before losing scope

        try
        {
            return new(socket, remoteEndPoint, inputPipeOptions, outputPipeOptions);
        }
        catch
        {
            using (socket) throw;
        }
    }
}