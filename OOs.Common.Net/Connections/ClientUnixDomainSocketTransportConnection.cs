using System.IO.Pipelines;
using System.Net.Sockets;

#nullable enable

namespace OOs.Net.Connections;

public sealed class ClientUnixDomainSocketTransportConnection(Socket socket, UnixDomainSocketEndPoint remoteEndPoint,
    PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) :
    ClientSocketTransportConnection(socket, remoteEndPoint, inputPipeOptions, outputPipeOptions)
{
    public override string ToString() => $"{Id}-UD ({RemoteEndPoint?.ToString() ?? "Not connected"})";

    public static ClientUnixDomainSocketTransportConnection Create(UnixDomainSocketEndPoint remoteEndPoint,
        PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null)
    {
        ArgumentNullException.ThrowIfNull(remoteEndPoint);

#pragma warning disable CA2000 // Dispose objects before losing scope
        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
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