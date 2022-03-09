using System.Runtime.CompilerServices;
using static System.Properties.Strings;
using static System.Threading.Tasks.TaskCreationOptions;

namespace System.Threading;

#nullable enable

public class AsyncSemaphore
{
    private static Action<object?, CancellationToken>? cancelCallback;
    private readonly int maxCount;
    private readonly bool runContinuationsAsynchronously;
    private readonly object syncRoot;
    private int currentCount;
    private WaiterNode? head;
    private WaiterNode? tail;
    private int waitersCount;

    public AsyncSemaphore(int initialCount, int maxCount = int.MaxValue, bool runContinuationsAsynchronously = true)
    {
        if (initialCount < 0 || initialCount > maxCount)
        {
            throw new ArgumentOutOfRangeException(nameof(initialCount), initialCount, AsyncSemaphoreCtorInitialCountWrong);
        }

        if (maxCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, AsyncSemaphoreCtorMaxCountWrong);
        }

        currentCount = initialCount;
        this.maxCount = maxCount;
        this.runContinuationsAsynchronously = runContinuationsAsynchronously;
        syncRoot = new();
    }

    public int MaxCount => maxCount;

    public int CurrentCount => Volatile.Read(ref currentCount) + waitersCount;

    public Task WaitAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        if (Interlocked.Decrement(ref currentCount) >= 0)
        {
            return Task.CompletedTask;
        }

        lock (syncRoot)
        {
            var waiter = new WaiterNode(runContinuationsAsynchronously);

            waitersCount++;

            if (head is null)
            {
                head = tail = waiter;
            }
            else
            {
                waiter.Prev = tail;
                tail = tail!.Next = waiter;
            }

            return cancellationToken == default ? waiter.Task : WaitCoreAsync(waiter, cancellationToken);
        }
    }

    private async Task WaitCoreAsync(WaiterNode waiter, CancellationToken cancellationToken)
    {
        using (cancellationToken.UnsafeRegister(cancelCallback ??= CancelWaiter, waiter))
        {
            await waiter.Task.ConfigureAwait(false);
        }
    }

    private void CancelWaiter(object? state, CancellationToken token)
    {
        var waiter = (WaiterNode)state!;

        if (!waiter.TrySetCanceled(token)) return;

        Interlocked.Increment(ref currentCount);

        lock (syncRoot)
        {
            var prev = waiter.Prev;
            if (prev is not null)
            {
                prev.Next = waiter.Next;
            }

            var next = waiter.Next;
            if (next is not null)
            {
                next.Prev = waiter.Prev;
            }

            if (head == waiter)
            {
                head = waiter.Next;
            }

            if (tail == waiter)
            {
                tail = waiter.Prev;
            }

            waiter.Prev = waiter.Next = null;

            waitersCount--;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Release() => Release(1);

    public void Release(int releaseCount)
    {
        var current = Interlocked.Add(ref currentCount, releaseCount);
        if (current > maxCount)
        {
            Interlocked.Add(ref currentCount, -releaseCount);
            throw new SemaphoreFullException();
        }

        if (current <= 0)
        {
            lock (syncRoot)
            {
                while (releaseCount-- > 0)
                {
                    var waiter = head;

                    if (waiter is null) continue;

                    waiter.TrySetResult();

                    head = waiter.Next;
                    waiter.Next = null;

                    if (head is not null)
                    {
                        head.Prev = null;
                    }

                    if (tail == waiter)
                    {
                        tail = null;
                    }

                    waitersCount--;
                }
            }
        }
    }

    private class WaiterNode : TaskCompletionSource
    {
        public WaiterNode(bool runContinuationsAsynchronously) :
            base(runContinuationsAsynchronously ? RunContinuationsAsynchronously : None)
        { }

        public WaiterNode? Next { get; set; }

        public WaiterNode? Prev { get; set; }
    }
}