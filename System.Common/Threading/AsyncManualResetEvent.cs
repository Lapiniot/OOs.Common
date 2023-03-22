namespace System.Threading;

/// <summary>
/// Represent lightweight synchronization event that must be reset manually.
/// Unlike <see cref="ManualResetEventSlim" /> this implementation supports only
/// asynchronous scenarios via calls to <see cref="WaitAsync(CancellationToken)" />
/// that returns awaitable task.
/// </summary>
public sealed class AsyncManualResetEvent
{
    private volatile TaskCompletionSource tcs;
    private nuint resetGuard;

    public AsyncManualResetEvent(bool initialState = false)
    {
        tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        if (initialState)
        {
            tcs.SetResult();
        }
    }

    public void Set() => tcs.TrySetResult();

    public void Reset()
    {
        SpinWait spinner = default;

        while (true)
        {
            if (!tcs.Task.IsCompleted)
                return;

            // We must limit concurrent writes to the tcs field 
            // in order to avoid orphaned tasks!!!
            if (Interlocked.CompareExchange(ref resetGuard, 1, 0) == 0)
            {
                // We acquired exclusive access to the tcs field.
                // Update it if still needed, release guard lock and exit asap
                if (tcs.Task.IsCompleted)
                {
                    tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
                }

                Interlocked.Exchange(ref resetGuard, 0);
                return;
            }

            spinner.SpinOnce(sleep1Threshold: -1);
        }
    }

    public Task WaitAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return tcs.Task.WaitAsync(cancellationToken);
    }
}