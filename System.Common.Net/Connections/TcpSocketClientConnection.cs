namespace System.Net.Connections;

public sealed class TcpSocketClientConnection : SocketConnection
{
    private readonly string hostNameOrAddress;
    private readonly int port;

    public TcpSocketClientConnection(IPEndPoint remoteEndPoint) : base(remoteEndPoint, Sockets.ProtocolType.Tcp)
    { }

    public TcpSocketClientConnection(string hostNameOrAddress, int port)
    {
        ArgumentException.ThrowIfNullOrEmpty(hostNameOrAddress);

        this.hostNameOrAddress = hostNameOrAddress;
        this.port = port;
    }

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        var remoteEndPoint = RemoteEndPoint ?? await ResolveRemoteEndPointAsync(hostNameOrAddress, port, cancellationToken).ConfigureAwait(false);
        await ConnectAsClientAsync(remoteEndPoint, cancellationToken).ConfigureAwait(false);
    }

    public override string ToString() => $"{Id}-TCP ({RemoteEndPoint?.ToString() ?? "Not connected"})";
}