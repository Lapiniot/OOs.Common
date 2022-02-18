using System.Runtime.CompilerServices;

namespace System;

/// <summary>
/// Implements async state management template for any type which runs some activity and may be in two states only
/// (Running/Stopped, Connected/Disconnected e.g.).
/// </summary>
public abstract class ActivityObject : IAsyncDisposable
{
    private readonly SemaphoreSlim semaphore = new(1);
    private int disposed;

    protected bool IsRunning { get; private set; }

    protected abstract Task StartingAsync(CancellationToken cancellationToken);

    protected abstract Task StoppingAsync();

    protected void CheckState(bool state, [CallerMemberName] string callerName = null)
    {
        if (IsRunning != state)
        {
            throw new InvalidOperationException($"Cannot call '{callerName}' in the current state.");
        }
    }

    protected void CheckDisposed()
    {
        if (Volatile.Read(ref disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(ActivityObject));
        }
    }

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
                _ = semaphore.Release();
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
                {
                    await StoppingAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                IsRunning = false;
                _ = semaphore.Release();
            }
        }
    }

    #region Implementation of IAsyncDisposable

    public virtual async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref disposed, 1, 0) != 0) return;

        GC.SuppressFinalize(this);

        using (semaphore)
        {
            await StopActivityCoreAsync().ConfigureAwait(false);
        }
    }

    #endregion
}