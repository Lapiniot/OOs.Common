using System.IO.Pipelines;
using System.Net.Connections;

namespace System.Net.Pipelines;

/// <summary>
/// Provides generic pipe data producer which reads data from abstract <seealso cref="INetworkConnection" />
/// on data arrival and writes it to the pipe. Reads by consumers are supported via
/// implemented <seealso cref="PipeReader" /> methods.
/// </summary>
public sealed class NetworkTransportPipe : TransportPipe
{
    private readonly INetworkConnection connection;

    public NetworkTransportPipe(INetworkConnection connection, PipeOptions pipeOptions = null) : base(pipeOptions)
    {
        ArgumentNullException.ThrowIfNull(connection);
        this.connection = connection;
    }

    protected override ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken) =>
        connection.ReceiveAsync(buffer, cancellationToken);

    protected override ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) =>
        connection.SendAsync(buffer, cancellationToken);
}