namespace System.Net.Connections;

public sealed class TcpClientSocketConnection : TcpSocketConnection
{
    private readonly string hostNameOrAddress;
    private readonly int port;

    public TcpClientSocketConnection(IPEndPoint remoteEndPoint) : base(remoteEndPoint, Sockets.ProtocolType.Tcp)
    { }

    public TcpClientSocketConnection(string hostNameOrAddress, int port)
    {
        Verify.ThrowIfNullOrEmpty(hostNameOrAddress);

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