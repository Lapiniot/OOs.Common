using System.Net.Connections.Exceptions;
using System.Net.Sockets;
using static System.Net.Dns;
using static System.Net.Properties.Strings;
using static System.Net.Sockets.ProtocolType;
using static System.Net.Sockets.SocketError;
using static System.Net.Sockets.SocketFlags;

namespace System.Net.Connections;

public class TcpClientSocketConnection : NetworkConnection
{
    private readonly string hostNameOrAddress;
    private readonly int port;
    private Socket socket;
    private long disposed;

    public TcpClientSocketConnection(IPEndPoint ipEndPoint)
    {
        ArgumentNullException.ThrowIfNull(ipEndPoint);
        RemoteEndPoint = ipEndPoint;
    }

    public TcpClientSocketConnection(string hostNameOrAddress, int port)
    {
        ArgumentNullException.ThrowIfNull(hostNameOrAddress);

        if(hostNameOrAddress.Length == 0) throw new ArgumentException(NotEmptyExpected, nameof(hostNameOrAddress));
        this.hostNameOrAddress = hostNameOrAddress;
        this.port = port;
    }

    public IPEndPoint RemoteEndPoint { get; private set; }
    protected Socket Socket { get => socket; set => socket = value; }

    public override async ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        try
        {
            await socket.SendAsync(buffer, None, cancellationToken).ConfigureAwait(false);
        }
        catch(SocketException se) when(se.SocketErrorCode is ConnectionAborted or ConnectionReset)
        {
            await StopActivityAsync().ConfigureAwait(false);
            throw new ConnectionAbortedException(se);
        }
    }

    public override async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        try
        {
            return await socket.ReceiveAsync(buffer, None, cancellationToken).ConfigureAwait(false);
        }
        catch(SocketException se) when(se.SocketErrorCode is ConnectionAborted or ConnectionReset)
        {
            await StopActivityAsync().ConfigureAwait(false);
            throw new ConnectionAbortedException(se);
        }
    }

    public override async ValueTask DisposeAsync()
    {
        if(Interlocked.CompareExchange(ref disposed, 1, 0) != 0) return;

        GC.SuppressFinalize(this);

        using(socket)
        {
            try
            {
                await base.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                if(socket.Connected)
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
            }
        }
    }

    protected override async Task StoppingAsync()
    {
        await socket.DisconnectAsync(true).ConfigureAwait(false);
    }

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        try
        {
            if(RemoteEndPoint is null)
            {
                var addresses = await GetHostAddressesAsync(hostNameOrAddress, cancellationToken).ConfigureAwait(false);
                RemoteEndPoint = new IPEndPoint(addresses[0], port);
            }

            socket ??= new Socket(RemoteEndPoint.AddressFamily, SocketType.Stream, Tcp);

            await socket.ConnectAsync(RemoteEndPoint, cancellationToken).ConfigureAwait(false);
        }
        catch(SocketException se) when(se.SocketErrorCode == HostNotFound)
        {
            throw new HostNotFoundException(se);
        }
        catch(SocketException se)
        {
            throw new ServerUnavailableException(se);
        }
    }

    public override string ToString()
    {
        return $"{nameof(TcpClientSocketConnection)}: {socket?.RemoteEndPoint?.ToString() ?? "Not connected"}";
    }
}