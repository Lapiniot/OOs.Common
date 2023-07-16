namespace System.Threading;

/// <summary>
/// Base class for types that run some work asynchronously in the background
/// </summary>
public abstract class Worker : IAsyncDisposable
{
    private readonly SemaphoreSlim semaphore = new(1);
    private CancelableOperationScope cancelableOperation;
    private int disposed;

    public bool IsRunning => cancelableOperation is not null;

    /// <summary>
    /// Must be implemented by derived type and represents actual asynchronous operation to be run on background
    /// </summary>
    /// <param name="stoppingToken"><see cref="CancellationToken" /> for cancellation signaling</param>
    /// <returns>Awaitable task, representing actual background work</returns>
    protected abstract Task ExecuteAsync(CancellationToken stoppingToken);

    /// <summary>
    /// Starts work and returns task that represents running asynchronous work
    /// </summary>
    /// <param name="stoppingToken"><see cref="CancellationToken" /> that signals about external cancellation</param>
    /// <returns>Awaitable task that represents currently running operation</returns>
    public async Task RunAsync(CancellationToken stoppingToken)
    {
        CheckDisposed();

        await semaphore.WaitAsync(stoppingToken).ConfigureAwait(false);

        CancelableOperationScope operation;

        try
        {
            operation = cancelableOperation ??= CancelableOperationScope.Start(ct => ExecuteAsync(ct), stoppingToken);
        }
        finally
        {
            semaphore.Release();
        }

        await operation.Completion.ConfigureAwait(false);
    }

    /// <summary>
    /// Signals currently running asynchronous work about completion request
    /// </summary>
    /// <returns>Awaitable task which represents result of background work completion</returns>
    public Task StopAsync()
    {
        CheckDisposed();
        return StopCoreAsync();
    }

    private async Task StopCoreAsync()
    {
        await semaphore.WaitAsync().ConfigureAwait(false);

        try
        {
            await using (cancelableOperation)
            {
                cancelableOperation = null;
            }
        }
#pragma warning disable CA1031
        catch
        {
            // Should not throw loop-breaking exception here by design
        }
#pragma warning restore CA1031
        finally
        {
            semaphore.Release();
        }
    }

    protected void CheckDisposed() => ObjectDisposedException.ThrowIf(disposed is 1, this);

    #region Implementation of IAsyncDisposable

    public virtual async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref disposed, 1, 0) != 0) return;

        GC.SuppressFinalize(this);

        using (semaphore)
        {
            await StopCoreAsync().ConfigureAwait(false);
        }
    }

    #endregion
}