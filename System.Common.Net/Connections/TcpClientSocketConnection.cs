using static System.Net.Properties.Strings;

namespace System.Net.Connections;

public class TcpClientSocketConnection : TcpSocketConnection
{
    private readonly string hostNameOrAddress;
    private readonly int port;

    public TcpClientSocketConnection(IPEndPoint remoteEndPoint)
    {
        ArgumentNullException.ThrowIfNull(remoteEndPoint);
        RemoteEndPoint = remoteEndPoint;
    }

    public TcpClientSocketConnection(string hostNameOrAddress, int port)
    {
        if (string.IsNullOrEmpty(hostNameOrAddress)) throw new ArgumentException(NotEmptyExpected, nameof(hostNameOrAddress));
        this.hostNameOrAddress = hostNameOrAddress;
        this.port = port;
    }

    protected override async Task StartingAsync(CancellationToken cancellationToken) =>
        await ConnectAsClientAsync(RemoteEndPoint ??
            (await ResolveRemoteEndPointAsync(hostNameOrAddress, port, cancellationToken).ConfigureAwait(false)),
            cancellationToken).ConfigureAwait(false);

    public override string ToString() => $"{Id}-TCP-{RemoteEndPoint?.ToString() ?? "Not connected"}";
}