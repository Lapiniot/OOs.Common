namespace System.Net.Connections;

public abstract class NetworkConnection : ActivityObject, INetworkConnection
{
    protected NetworkConnection()
    {
        Id = Base32.ToBase32String(CorrelationIdGenerator.GetNext());
    }

    public abstract ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);

    public abstract ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);

    #region Implementation of IConnectedObject

    public bool IsConnected => IsRunning;

    public string Id { get; }

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        return StartActivityAsync(cancellationToken);
    }

    public Task DisconnectAsync()
    {
        return StopActivityAsync();
    }

    #endregion

    public override string ToString()
    {
        return $"{Id}-{GetType().Name}";
    }
}