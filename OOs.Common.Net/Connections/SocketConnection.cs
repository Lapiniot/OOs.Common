using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using static System.Net.Sockets.SocketFlags;
using static System.Net.Sockets.SocketError;
using System.Net;

namespace OOs.Net.Connections;

public abstract class SocketConnection : NetworkConnection
{
    private readonly ProtocolType protocolType;
    private int disposed;
    private EndPoint remoteEndPoint;
    private EndPoint localEndPoint;
    private Socket socket;

    protected SocketConnection()
    { }

    protected SocketConnection(Socket acceptedSocket)
    {
        ArgumentNullException.ThrowIfNull(acceptedSocket);
        Socket = acceptedSocket;
        remoteEndPoint = acceptedSocket.RemoteEndPoint;
        localEndPoint = acceptedSocket.LocalEndPoint;
    }

    protected SocketConnection(EndPoint remoteEndPoint, ProtocolType protocolType)
    {
        ArgumentNullException.ThrowIfNull(remoteEndPoint);
        this.remoteEndPoint = remoteEndPoint;
        this.protocolType = protocolType;
    }

    protected Socket Socket { get => socket; set => socket = value; }

    public sealed override EndPoint RemoteEndPoint => remoteEndPoint;

    public sealed override EndPoint LocalEndPoint => localEndPoint;

    public override async ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        try
        {
            await socket.SendAsync(buffer, None, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException se) when (se.SocketErrorCode is ConnectionAborted or ConnectionReset or Shutdown)
        {
            ThrowConnectionClosed(se);
        }
    }

    public override async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        try
        {
            return await socket.ReceiveAsync(buffer, None, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException se) when (se.SocketErrorCode is ConnectionAborted or ConnectionReset or Shutdown)
        {
            ThrowConnectionClosed(se);
            return 0;
        }
    }

    protected override async Task StoppingAsync()
    {
        socket.Shutdown(SocketShutdown.Both);
        await socket.DisconnectAsync(false).ConfigureAwait(false);
    }

    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;

        GC.SuppressFinalize(this);

        using (socket)
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }

    public override string ToString() => $"{Id}-TCP ({remoteEndPoint})";

    protected static async Task<IPEndPoint> ResolveRemoteEndPointAsync(string hostNameOrAddress, int port, CancellationToken cancellationToken)
    {
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(hostNameOrAddress, cancellationToken).ConfigureAwait(false);
            return new(addresses[0], port);
        }
        catch (SocketException se) when (se.SocketErrorCode == HostNotFound)
        {
            ThrowHostNotFound(se);
            return default;
        }
    }

    protected async Task ConnectAsClientAsync([NotNull] EndPoint endPoint, CancellationToken cancellationToken)
    {
        try
        {
            if (socket is not null && socket.AddressFamily != endPoint.AddressFamily)
            {
                socket.Close();
                socket = null;
            }

            socket ??= new(endPoint.AddressFamily, SocketType.Stream, protocolType);
            await socket.ConnectAsync(endPoint, cancellationToken).ConfigureAwait(false);
            remoteEndPoint = socket.RemoteEndPoint;
            localEndPoint = socket.LocalEndPoint;
        }
        catch (SocketException se)
        {
            ThrowServerUnavailable(se);
        }
    }
}