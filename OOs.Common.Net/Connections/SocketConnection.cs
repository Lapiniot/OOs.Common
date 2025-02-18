using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using static System.Net.Sockets.SocketError;
using static System.Net.Sockets.SocketFlags;

namespace OOs.Net.Connections;

public abstract class SocketConnection(bool reuseSocket) : NetworkConnection
{
    private readonly ProtocolType protocolType;
    private readonly bool reuseSocket = reuseSocket;
    private int disposed;
    private EndPoint remoteEndPoint;
    private EndPoint localEndPoint;
    private Socket socket;

    protected SocketConnection(Socket socket, bool reuseSocket) : this(reuseSocket)
    {
        ArgumentNullException.ThrowIfNull(socket);
        Socket = socket;
        remoteEndPoint = socket.RemoteEndPoint;
        localEndPoint = socket.LocalEndPoint;
    }

    protected SocketConnection(EndPoint remoteEndPoint, ProtocolType protocolType, bool reuseSocket) : this(reuseSocket)
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
            ThrowHelper.ThrowConnectionClosed(se);
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
            ThrowHelper.ThrowConnectionClosed(se);
            return 0;
        }
    }

    protected override async Task StoppingAsync()
    {
        socket.Shutdown(SocketShutdown.Both);
        await socket.DisconnectAsync(reuseSocket).ConfigureAwait(false);
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
            ThrowHelper.ThrowHostNotFound(se);
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
            ThrowHelper.ThrowServerUnavailable(se);
        }
    }
}