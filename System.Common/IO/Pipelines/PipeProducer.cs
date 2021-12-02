using System.Properties;

namespace System.IO.Pipelines;

public abstract class PipeProducer : IAsyncDisposable
{
    private const int Stopped = 0;
    private const int Started = 1;
    private const int Stopping = 2;
    private bool disposed;
    private CancellationTokenSource globalCts;
    private readonly PipeReader reader;
    private readonly PipeWriter writer;
    private readonly Pipe pipe;
    private Task producer;
    private long stateGuard;

    public PipeReader Reader => reader;

    public Task Completion => Interlocked.Read(ref stateGuard) != Started
        ? throw new InvalidOperationException(Strings.InvalidStateNotStarted)
        : producer;

    protected PipeProducer(PipeOptions pipeOptions = null)
    {
        pipe = new(pipeOptions ?? new PipeOptions(useSynchronizationContext: false));
        (reader, writer) = pipe;
    }

    #region Implementation of IAsyncDisposable

    public async ValueTask DisposeAsync()
    {
        if(disposed) return;

        GC.SuppressFinalize(this);

        try
        {
            await StopAsync().ConfigureAwait(false);
        }
        finally
        {
            disposed = true;
        }
    }

    #endregion

    public void Start()
    {
        CheckDisposed();

        switch(Interlocked.CompareExchange(ref stateGuard, Started, Stopped))
        {
            case Stopped:
                var cts = new CancellationTokenSource();
                globalCts = cts;
                pipe.Reset();
                producer = StartProducerAsync(writer, cts.Token);
                break;
            case Stopping:
                throw new InvalidOperationException("Cannot start in this state, currently in stopping transition.");
        }
    }

    public async ValueTask StopAsync()
    {
        var localWorker = producer;
        var localCts = globalCts;

        switch(Interlocked.CompareExchange(ref stateGuard, Stopping, Started))
        {
            case Started:
                // we are responsible for cancellation and cleanup
                localCts.Cancel();
                try
                {
                    await localWorker.ConfigureAwait(false);
                }
                finally
                {
                    localCts.Dispose();
                    _ = Interlocked.Exchange(ref stateGuard, Stopped);
                }

                break;
            case Stopping:
                // stopping in progress already, wait for currently active task in flight
                await localWorker.ConfigureAwait(false);
                break;
        }
    }

    private void CheckDisposed()
    {
        if(disposed) throw new InvalidOperationException(Strings.ObjectInstanceDisposed);
    }

    private async Task StartProducerAsync(PipeWriter writer, CancellationToken token)
    {
        try
        {
            while(!token.IsCancellationRequested)
            {
                var buffer = writer.GetMemory();

                var rt = ReceiveAsync(buffer, token);
                var received = rt.IsCompletedSuccessfully ? rt.Result : await rt.ConfigureAwait(false);

                if(received == 0)
                {
                    break;
                }

                writer.Advance(received);

                var ft = writer.FlushAsync(token);
                var result = ft.IsCompletedSuccessfully ? ft.Result : await ft.ConfigureAwait(false);

                if(result.IsCompleted || result.IsCanceled)
                {
                    break;
                }
            }

        }
        catch(OperationCanceledException)
        {
            // Expected
        }
        finally
        {
            await writer.CompleteAsync().ConfigureAwait(false);
        }
    }

    protected abstract ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);
}