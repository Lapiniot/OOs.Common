using System.IO.Pipelines;
using OOs.IO.Pipelines;
using OOs.Net.Connections;

namespace OOs.Net.Pipelines;

/// <summary>
/// Provides generic pipe data producer which reads data from abstract <seealso cref="INetworkConnection" />
/// on data arrival and writes it to the pipe. Reads by consumers are supported via
/// implemented <seealso cref="PipeReader" /> methods.
/// </summary>
public sealed class NetworkTransportPipe : TransportPipe
{
    private readonly INetworkConnection connection;

    public NetworkTransportPipe(INetworkConnection connection, PipeOptions inputPipeOptions = null, PipeOptions outputPipeOptions = null) :
        base(inputPipeOptions, outputPipeOptions)
    {
        ArgumentNullException.ThrowIfNull(connection);
        this.connection = connection;
    }

    protected override ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken) =>
        connection.ReceiveAsync(buffer, cancellationToken);

    protected override ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) =>
        connection.SendAsync(buffer, cancellationToken);

    public override string ToString() => connection.ToString();
}