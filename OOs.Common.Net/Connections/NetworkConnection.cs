using System.Net;

namespace OOs.Net.Connections;

public abstract class NetworkConnection : ActivityObject, INetworkConnection
{
    public string Id { get; } = Base32.ToBase32String(CorrelationIdGenerator.GetNext());
    public abstract EndPoint LocalEndPoint { get; }
    public abstract EndPoint RemoteEndPoint { get; }
    public override string ToString() => $"{Id}-{GetType().Name}";

    public Task ConnectAsync(CancellationToken cancellationToken = default) => StartActivityAsync(cancellationToken);

    public Task DisconnectAsync() => StopActivityAsync();

    public abstract ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);

    public abstract ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);
}