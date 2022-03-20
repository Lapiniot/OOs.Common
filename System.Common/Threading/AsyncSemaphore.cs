using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using static System.Properties.Strings;
using static System.Threading.Tasks.TaskCreationOptions;

namespace System.Threading;

#nullable enable

public sealed class AsyncSemaphore
{
    private readonly int maxCount;
    private readonly bool runContinuationsAsynchronously;
    private readonly object syncRoot;
    private Action<object?, CancellationToken>? cancelCallback;
    private int currentCount;
    private WaiterNode? head;
    private WaiterNode? tail;

    public AsyncSemaphore(int initialCount, int maxCount = int.MaxValue, bool runContinuationsAsynchronously = true)
    {
        if (initialCount < 0 || initialCount > maxCount)
            throw new ArgumentOutOfRangeException(nameof(initialCount), initialCount, AsyncSemaphoreCtorInitialCountWrong);

        if (maxCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, AsyncSemaphoreCtorMaxCountWrong);

        currentCount = initialCount;
        this.maxCount = maxCount;
        this.runContinuationsAsynchronously = runContinuationsAsynchronously;
        syncRoot = new();
    }

    public int MaxCount => maxCount;

    public int CurrentCount
    {
        get
        {
            var current = Volatile.Read(ref currentCount);
            return current >= 0 ? current : 0;
        }
    }

    public Task WaitAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        if (Interlocked.Decrement(ref currentCount) >= 0)
            return Task.CompletedTask;

        lock (syncRoot)
        {
            var waiter = new WaiterNode(runContinuationsAsynchronously);
            Enqueue(waiter);

            if (cancellationToken != CancellationToken.None)
            {
                waiter.CtReg = cancellationToken.UnsafeRegister(cancelCallback ??= CancelWaiter, waiter);
            }

            return waiter.Task;
        }
    }

    private void CancelWaiter(object? state, CancellationToken token)
    {
        var waiter = (WaiterNode)state!;

        if (waiter.TrySetCanceled(token))
        {
            Interlocked.Increment(ref currentCount);

            lock (syncRoot)
            {
                TryRemove(waiter);
            }

            waiter.CtReg.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Release() => Release(1);

    public void Release(int releaseCount)
    {
        int localCount;
        var sw = new SpinWait();

        while (true)
        {
            localCount = currentCount;

            if (localCount + releaseCount > maxCount)
                throw new SemaphoreFullException();

            if (Interlocked.CompareExchange(ref currentCount, localCount + releaseCount, localCount) == localCount)
                break;

            sw.SpinOnce(sleep1Threshold: -1);
        }

        if (localCount >= 0)
            return;

        lock (syncRoot)
        {
            while (releaseCount > 0 && TryDequeue(out var waiter))
            {
                if (waiter.TrySetResult())
                {
                    waiter.CtReg.Dispose();
                    releaseCount--;
                }
            }
        }
    }

    private void Enqueue(WaiterNode waiter)
    {
        if (head is null)
        {
            head = tail = waiter;
        }
        else
        {
            waiter.Prev = tail;
            tail = tail!.Next = waiter;
        }
    }

    private bool TryRemove(WaiterNode waiter)
    {
        if (waiter is { Next: null, Prev: null } && waiter != head)
            return false;

        var prev = waiter.Prev;
        if (prev is not null)
            prev.Next = waiter.Next;

        var next = waiter.Next;
        if (next is not null)
            next.Prev = waiter.Prev;

        if (head == waiter)
            head = waiter.Next;

        if (tail == waiter)
            tail = waiter.Prev;

        waiter.Prev = waiter.Next = null;

        return true;
    }

    private bool TryDequeue([NotNullWhen(true)] out WaiterNode? waiter)
    {
        waiter = head;

        if (waiter is null)
            return false;

        head = waiter.Next;
        waiter.Next = waiter.Prev = null;

        if (head is not null)
            head.Prev = null;

        if (tail == waiter)
            tail = null;

        return true;
    }

    private sealed class WaiterNode : TaskCompletionSource
    {
        public WaiterNode(bool runContinuationsAsynchronously) :
            base(runContinuationsAsynchronously ? RunContinuationsAsynchronously : None)
        { }

        public WaiterNode? Next { get; set; }
        public WaiterNode? Prev { get; set; }
        public CancellationTokenRegistration CtReg { get; set; }
    }
}