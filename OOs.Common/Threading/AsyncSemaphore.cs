﻿using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using OOs.Diagnostics;
using static System.Threading.Tasks.TaskCreationOptions;
#if NET8_0
using Lock = System.Object;
#endif

namespace OOs.Threading;

#nullable enable

public sealed class AsyncSemaphore : IProvideInstrumentationMetrics
{
    private static long waitingCount;
    private readonly bool runContinuationsAsynchronously;
    private readonly Lock syncRoot;
    private Action<object?, CancellationToken>? cancelCallback;
    private int maxCount;
    private int currentCount;
    private WaiterNode? head;
    private WaiterNode? tail;

    public AsyncSemaphore(int initialCount, int maxCount = int.MaxValue, bool runContinuationsAsynchronously = true)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxCount, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCount, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialCount, maxCount);

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
            if (RuntimeOptions.ThreadingInstrumentationSupported)
                Interlocked.Increment(ref waitingCount);
            var waiter = new WaiterNode(runContinuationsAsynchronously);
            if (cancellationToken != CancellationToken.None)
                waiter.CtReg = cancellationToken.UnsafeRegister(cancelCallback ??= CancelWaiter, waiter);
            Enqueue(waiter);
            return waiter.Task;
        }
    }

    private void CancelWaiter(object? state, CancellationToken token)
    {
        var waiter = (WaiterNode)state!;
        if (!waiter.TrySetCanceled(token))
            return;

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
            ThrowHelper.ThrowSemaphoreFull();
    }

    public bool TryRelease(int releaseCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(releaseCount, 1);

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
                if (!waiter.TrySetResult())
                    continue;
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
        if (waiter is { Next: null, Prev: null } && waiter != head)
            return;
        var prev = waiter.Prev;
        var next = waiter.Next;
        prev?.Next = waiter.Next;
        next?.Prev = waiter.Prev;
        if (head == waiter)
            head = waiter.Next;
        if (tail == waiter)
            tail = waiter.Prev;
        waiter.Prev = waiter.Next = null;
    }

    private bool TryDequeue([NotNullWhen(true)] out WaiterNode? waiter)
    {
        waiter = head;
        if (waiter is null)
            return false;
        head = waiter.Next;
        waiter.Next = waiter.Prev = null;
        head?.Prev = null;
        if (tail == waiter)
            tail = null;
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
        if (RuntimeOptions.ThreadingInstrumentationSupported)
        {
            var meter = new Meter(meterName ?? "OOs.Threading.AsyncSemaphore");
            meter.CreateObservableGauge("waiting-count", static () => waitingCount, description: "Number of asynchronous waits");
            return meter;
        }

        return null;
    }

    /// <summary>
    /// Tries to reset current instance and initialize it with new <paramref name="initialCount"/> and 
    /// <paramref name="maxCount"/> values.
    /// </summary>
    /// <param name="initialCount">New value for <see cref="CurrentCount"/>.</param>
    /// <param name="maxCount">New value for <see cref="MaxCount"/>.</param>
    /// <returns>
    /// Returns <see langword="true"/> if current instance doesn't track 
    /// pending asynchronouse waiters and can be reset for potential reuse.
    /// </returns>
    public bool TryReset(int initialCount, int maxCount = int.MaxValue)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxCount, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCount, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialCount, maxCount);

        lock (syncRoot)
        {
            if (head is null)
            {
                currentCount = initialCount;
                this.maxCount = maxCount;
                return true;
            }
        }

        return false;
    }
}