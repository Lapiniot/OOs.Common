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

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
}