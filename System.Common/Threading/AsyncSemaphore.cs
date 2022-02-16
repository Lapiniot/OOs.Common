using System.Runtime.CompilerServices;
using static System.Properties.Strings;
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
        if(initialCount < 0 || initialCount > maxCount)
        {
            throw new ArgumentOutOfRangeException(nameof(initialCount), initialCount, AsyncSemaphoreCtorInitialCountWrong);
        }

        if(maxCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, AsyncSemaphoreCtorMaxCountWrong);
        }

        currentCount = initialCount;
        this.maxCount = maxCount;
        waiters = new Queue<TaskCompletionSource>();
        syncRoot = new object();
    }

    public int MaxCount => maxCount;

    public int CurrentCount => Volatile.Read(ref currentCount);

    public Task WaitAsync(CancellationToken cancellationToken = default)
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
            if(currentCount + releaseCount - waitersCount > maxCount)
            {
                throw new SemaphoreFullException();
            }

            while(waitersCount > 0 && releaseCount > 0 && waiters.TryDequeue(out var tcs))
            {
                waitersCount--;
                releaseCount--;
                tcs.SetResult();
            }

            currentCount += releaseCount;
        }
    }
}