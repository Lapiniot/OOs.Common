using System.IO.Pipelines;
using System.Net;

namespace OOs.Net.Connections;

/// <summary>
/// Provides generic pipe data producer which reads data from abstract <seealso cref="INetworkConnection" />
/// on data arrival and writes it to the pipe. Reads by consumers are supported via
/// implemented <seealso cref="PipeReader" /> methods.
/// </summary>
public sealed class NetworkConnectionAdapter : TransportConnectionPipeAdapter
{
    private readonly NetworkConnection connection;

    public NetworkConnectionAdapter(NetworkConnection connection, PipeOptions inputPipeOptions = null, PipeOptions outputPipeOptions = null) :
        base(inputPipeOptions, outputPipeOptions)
    {
        ArgumentNullException.ThrowIfNull(connection);
        this.connection = connection;
    }

    public override string Id => connection.Id;
    public override EndPoint LocalEndPoint => connection.LocalEndPoint;
    public override EndPoint RemoteEndPoint => connection.RemoteEndPoint;

    protected override ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken) =>
        connection.ReceiveAsync(buffer, cancellationToken);

    protected override ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) =>
        connection.SendAsync(buffer, cancellationToken);

    public override string ToString() => connection.ToString();

    public override async ValueTask DisposeAsync()
    {
        try
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }

    protected override async ValueTask OnStartingAsync(CancellationToken cancellationToken) =>
        await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);

    protected override async ValueTask OnStoppingAsync() =>
        await connection.DisconnectAsync().ConfigureAwait(false);
}