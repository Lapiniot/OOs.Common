using System.IO.Pipelines;
using System.Net;

#nullable enable

namespace OOs.Net.Connections;

public abstract class TransportConnection : IDuplexPipe, IAsyncDisposable
{
    public abstract PipeReader Input { get; }
    public abstract PipeWriter Output { get; }
    public abstract string Id { get; }
    public abstract EndPoint? LocalEndPoint { get; }
    public abstract EndPoint? RemoteEndPoint { get; }

    public abstract ValueTask StartAsync(CancellationToken cancellationToken);
    public abstract Task StopAsync();
    public abstract Task CompleteOutputAsync();

    public virtual ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}