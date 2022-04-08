using System.Net.Connections.Exceptions;
using System.Net.Sockets;
using static System.Net.Dns;
using static System.Net.Properties.Strings;
using static System.Net.Sockets.ProtocolType;
using static System.Net.Sockets.SocketError;

namespace System.Net.Connections;

public class TcpClientSocketConnection : TcpSocketConnection
{
    private readonly string hostNameOrAddress;
    private readonly int port;

    public TcpClientSocketConnection(IPEndPoint ipEndPoint)
    {
        ArgumentNullException.ThrowIfNull(ipEndPoint);
        RemoteEndPoint = ipEndPoint;
    }

    public TcpClientSocketConnection(string hostNameOrAddress, int port)
    {
        ArgumentNullException.ThrowIfNull(hostNameOrAddress);

        if (hostNameOrAddress.Length == 0) throw new ArgumentException(NotEmptyExpected, nameof(hostNameOrAddress));
        this.hostNameOrAddress = hostNameOrAddress;
        this.port = port;
    }

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (RemoteEndPoint is null)
            {
                var addresses = await GetHostAddressesAsync(hostNameOrAddress, cancellationToken).ConfigureAwait(false);
                RemoteEndPoint = new(addresses[0], port);
            }

            Socket ??= new(RemoteEndPoint.AddressFamily, SocketType.Stream, Tcp);

            await Socket.ConnectAsync(RemoteEndPoint, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException se) when (se.SocketErrorCode == HostNotFound)
        {
            throw new HostNotFoundException(se);
        }
        catch (SocketException se)
        {
            throw new ServerUnavailableException(se);
        }
    }

    public override string ToString() => $"{Id}-TCP-{RemoteEndPoint?.ToString() ?? "Not connected"}";
}