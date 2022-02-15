using System.Runtime.CompilerServices;
using static System.Threading.Tasks.TaskCreationOptions;

namespace System.Threading;

public class AsyncSemaphore
{
    private readonly int maxCount;
    private readonly object syncRoot;
    private readonly Queue<TaskCompletionSource> waiters;
    private int currentCount;
    private int waitersCount;

    public AsyncSemaphore(int initialCount, int maxCount = int.MaxValue)
    {
        currentCount = initialCount;
        this.maxCount = maxCount;
        waiters = new Queue<TaskCompletionSource>();
        syncRoot = new object();
    }

    public int CurrentCount => currentCount;

    public Task WaitAsync(CancellationToken cancellationToken)
    {
        if(cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock(syncRoot)
        {
            if(currentCount > 0)
            {
                currentCount--;
                return Task.CompletedTask;
            }

            waitersCount++;
            var tcs = new TaskCompletionSource(RunContinuationsAsynchronously);
            waiters.Enqueue(tcs);
            return tcs.Task.WaitAsync(cancellationToken);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Release()
    {
        Release(1);
    }

    public void Release(int releaseCount)
    {
        lock(syncRoot)
        {
            while(releaseCount-- > 0)
            {
                if(waitersCount > 0 && waiters.TryDequeue(out var tcs))
                {
                    waitersCount--;
                    tcs.TrySetResult();
                }
                else
                {
                    currentCount++;
                }
            }
        }
    }
}