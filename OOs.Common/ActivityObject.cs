using System.Runtime.CompilerServices;

namespace OOs;

/// <summary>
/// Provides state synchronization template base type for types running some activity and may be in two states only
/// (Running/Stopped, Connected/Disconnected e.g.).
/// </summary>
public abstract class ActivityObject : IAsyncDisposable
{
    private readonly SemaphoreSlim semaphore = new(1);
    private int disposed;

    protected bool IsRunning { get; private set; }

    protected abstract Task StartingAsync(CancellationToken cancellationToken);

    protected abstract Task StoppingAsync();

    protected void CheckState([CallerMemberName] string callerName = null)
    {
        if (!IsRunning)
            ThrowHelper.ThrowInvalidState(callerName);
    }

    protected void CheckDisposed() => ObjectDisposedException.ThrowIf(disposed is 1, this);

    protected async Task StartActivityAsync(CancellationToken cancellationToken)
    {
        CheckDisposed();

        if (!IsRunning)
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (!IsRunning)
                {
                    await StartingAsync(cancellationToken).ConfigureAwait(false);

                    IsRunning = true;
                }
            }
            finally
            {
                semaphore.Release();
            }
        }
    }

    protected Task StopActivityAsync()
    {
        CheckDisposed();
        return StopActivityCoreAsync();
    }

    private async Task StopActivityCoreAsync()
    {
        if (IsRunning)
        {
            await semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (IsRunning)
                    await StoppingAsync().ConfigureAwait(false);
            }
            finally
            {
                IsRunning = false;
                semaphore.Release();
            }
        }
    }

    #region Implementation of IAsyncDisposable

    public virtual async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;

        GC.SuppressFinalize(this);

        using (semaphore)
        {
            await StopActivityCoreAsync().ConfigureAwait(false);
        }
    }

    #endregion
}