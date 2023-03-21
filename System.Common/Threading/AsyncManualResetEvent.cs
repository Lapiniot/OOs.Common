namespace System.Threading;

/// <summary>
/// Represent lightweight synchronization event that must be reset manually.
/// Unlike <see cref="ManualResetEventSlim" /> this implementation supports only
/// asynchronous scenarios via calls to <see cref="WaitAsync(CancellationToken)" />
/// that returns awaitable task.
/// </summary>
public sealed class AsyncManualResetEvent
{
    private TaskCompletionSource tcs;
    private readonly object syncRoot = new();

    public AsyncManualResetEvent(bool initialState = false)
    {
        tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        if (initialState)
        {
            tcs.SetResult();
        }
    }

    public void Set()
    {
        lock (syncRoot)
        {
            tcs.TrySetResult();
        }
    }

    public void Reset()
    {
        lock (syncRoot)
        {
            if (!tcs.Task.IsCompleted)
                return;
            tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    public Task WaitAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return tcs.Task.WaitAsync(cancellationToken);
    }
}