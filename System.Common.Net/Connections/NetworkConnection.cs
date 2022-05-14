using System.Diagnostics.CodeAnalysis;
using System.Net.Connections.Exceptions;

namespace System.Net.Connections;

public abstract class NetworkConnection : ActivityObject, INetworkConnection
{
    protected NetworkConnection() => Id = Base32.ToBase32String(CorrelationIdGenerator.GetNext());

    public override string ToString() => $"{Id}-{GetType().Name}";

    [DoesNotReturn]
    protected static void ThrowConnectionClosed(Exception exception) =>
        throw new ConnectionClosedException(exception);

    [DoesNotReturn]
    protected static void ThrowHostNotFound(Exception exception) =>
        throw new HostNotFoundException(exception);

    [DoesNotReturn]
    protected static void ThrowServerUnavailable(Exception exception) =>
        throw new ServerUnavailableException(exception);

    public abstract ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);

    public abstract ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);

    #region Implementation of IConnectedObject

    public bool IsConnected => IsRunning;

    public string Id { get; }

    public Task ConnectAsync(CancellationToken cancellationToken = default) => StartActivityAsync(cancellationToken);

    public Task DisconnectAsync() => StopActivityAsync();

    #endregion
}