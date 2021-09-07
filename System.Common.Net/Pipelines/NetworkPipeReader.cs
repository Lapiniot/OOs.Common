using System.IO.Pipelines;
using System.Net.Connections;

namespace System.Net.Pipelines;

/// <summary>
/// Provides generic pipe data producer which reads data from abstract <seealso cref="INetworkConnection" />
/// on data arrival and writes it to the pipe. Reads by consumers are supported via
/// implemented <seealso cref="System.IO.Pipelines.PipeReader" /> methods.
/// </summary>
public sealed class NetworkPipeReader : PipeReaderBase
{
    private readonly INetworkConnection connection;

    public NetworkPipeReader(INetworkConnection connection, PipeOptions pipeOptions = null) : base(pipeOptions)
    {
        ArgumentNullException.ThrowIfNull(connection);
        this.connection = connection;
    }

    protected override ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        return connection.ReceiveAsync(buffer, cancellationToken);
    }
}