using System.Diagnostics.CodeAnalysis;
using System.Net;
using OOs.Net.Connections.Exceptions;

namespace OOs.Net.Connections;

public abstract class NetworkConnection : ActivityObject, INetworkConnection
{
    public string Id { get; } = Base32.ToBase32String(CorrelationIdGenerator.GetNext());
    public abstract EndPoint LocalEndPoint { get; }
    public abstract EndPoint RemoteEndPoint { get; }

    [DoesNotReturn]
    protected static void ThrowConnectionClosed(Exception exception) =>
        throw new ConnectionClosedException(exception);

    [DoesNotReturn]
    protected static void ThrowHostNotFound(Exception exception) =>
        throw new HostNotFoundException(exception);

    [DoesNotReturn]
    protected static void ThrowServerUnavailable(Exception exception) =>
        throw new ServerUnavailableException(exception);

    public override string ToString() => $"{Id}-{GetType().Name}";

    public Task ConnectAsync(CancellationToken cancellationToken = default) => StartActivityAsync(cancellationToken);

    public Task DisconnectAsync() => StopActivityAsync();

    public abstract ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);

    public abstract ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);
}