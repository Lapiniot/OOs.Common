using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using static System.Threading.Tasks.TaskCreationOptions;

namespace System.Threading;

#nullable enable

public sealed class AsyncSemaphore : IProvideInstrumentationMetrics
{
    private static long waitingCount;
    private readonly int maxCount;
    private readonly bool runContinuationsAsynchronously;
    private readonly object syncRoot;
    private Action<object?, CancellationToken>? cancelCallback;
    private int currentCount;
    private WaiterNode? head;
    private WaiterNode? tail;

    public AsyncSemaphore(int initialCount, int maxCount = int.MaxValue, bool runContinuationsAsynchronously = true)
    {
        Verify.ThrowIfLessOrEqual(maxCount, 0);
        Verify.ThrowIfNotInRange(initialCount, 0, maxCount);

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
            var current = currentCount;
            return current >= 0 ? current : 0;
        }
    }

    public Task WaitAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        lock (syncRoot)
        {
            if (--currentCount >= 0)
                return Task.CompletedTask;

            if (RuntimeSettings.ThreadingInstrumentationSupport)
            {
                Interlocked.Increment(ref waitingCount);
            }

            var waiter = new WaiterNode(runContinuationsAsynchronously);

            if (cancellationToken != CancellationToken.None)
            {
                waiter.CtReg = cancellationToken.UnsafeRegister(cancelCallback ??= CancelWaiter, waiter);
            }

            Enqueue(waiter);
            return waiter.Task;
        }
    }

    private void CancelWaiter(object? state, CancellationToken token)
    {
        var waiter = (WaiterNode)state!;

        if (!waiter.TrySetCanceled(token)) return;

        lock (syncRoot)
        {
            currentCount++;
            TryRemove(waiter);
        }

        waiter.CtReg.Dispose();
    }

    public void Release() => Release(1);

    public void Release(int releaseCount)
    {
        if (!TryRelease(releaseCount))
        {
            ThrowHelper.ThrowSemaphoreFull();
        }
    }

    public bool TryRelease(int releaseCount)
    {
        Verify.ThrowIfLess(releaseCount, 1);

        lock (syncRoot)
        {
            var current = currentCount;

            if (current + releaseCount > maxCount)
                return false;

            currentCount += releaseCount;

            if (current >= 0)
                return true;

            while (releaseCount > 0 && TryDequeue(out var waiter))
            {
                if (!waiter.TrySetResult()) continue;
                waiter.CtReg.Dispose();
                releaseCount--;
            }
        }

        return true;
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

    private void TryRemove(WaiterNode waiter)
    {
        if (waiter is { Next: null, Prev: null } && waiter != head) return;

        var prev = waiter.Prev;
        var next = waiter.Next;
        if (prev is not null) prev.Next = waiter.Next;
        if (next is not null) next.Prev = waiter.Prev;
        if (head == waiter) head = waiter.Next;
        if (tail == waiter) tail = waiter.Prev;
        waiter.Prev = waiter.Next = null;
    }

    private bool TryDequeue([NotNullWhen(true)] out WaiterNode? waiter)
    {
        waiter = head;
        if (waiter is null) return false;
        head = waiter.Next;
        waiter.Next = waiter.Prev = null;
        if (head is not null) head.Prev = null;
        if (tail == waiter) tail = null;
        return true;
    }

    private sealed class WaiterNode(bool runContinuationsAsynchronously) :
        TaskCompletionSource(runContinuationsAsynchronously ? RunContinuationsAsynchronously : None)
    {
        public WaiterNode? Next { get; set; }
        public WaiterNode? Prev { get; set; }
        public CancellationTokenRegistration CtReg { get; set; }
    }

    public static IDisposable? EnableInstrumentation(string? meterName = null)
    {
        if (RuntimeSettings.ThreadingInstrumentationSupport)
        {
            var meter = new Meter(meterName ?? "System.Threading.AsyncSemaphore");
            meter.CreateObservableGauge("waiting-count", static () => waitingCount, description: "Number of asynchronous waits");
            return meter;
        }

        return null;
    }
}