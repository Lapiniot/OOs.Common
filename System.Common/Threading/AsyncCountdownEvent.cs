#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace System.Threading;

public class AsyncCountdownEvent
{
    private readonly TaskCompletionSource completionSource;
    private readonly int initialCount;
    private int currentCount;

    public AsyncCountdownEvent(int signalCount)
    {
        Verify.ThrowIfLess(signalCount, 0);
        currentCount = initialCount = signalCount;
        completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        if (currentCount is 0) completionSource.TrySetResult();
    }

    public int CurrentCount
    {
        get
        {
            var current = Volatile.Read(ref currentCount);
            return current > 0 ? current : 0;
        }
    }

    public int InitialCount => initialCount;

    public void AddCount() => AddCount(1);

    public void AddCount(int signalCount) => throw new NotImplementedException();

    /// <summary>
    /// Signals current <see cref="AsyncCountdownEvent" /> instance, decrementing <see cref="CurrentCount" /> by one.
    /// </summary>
    /// <returns><see langword="true" /> if signal caused the <see cref="CurrentCount" /> to reach zero and the event was set, otherwise <see langword="false" />.</returns>
    /// <remarks>Method is thread-safe.</remarks>
    public bool Signal()
    {
        if (currentCount < 1) ThrowDecrementBelowZero();

        switch (Interlocked.Decrement(ref currentCount))
        {
            case 0:
                completionSource.TrySetResult();
                return true;
            case < 0:
                ThrowDecrementBelowZero();
                break;
        }

        return false;
    }

    /// <summary>
    /// Signals current <see cref="AsyncCountdownEvent" /> instance, decrementing <see cref="CurrentCount" /> by specified <paramref name="signalCount" /> value.
    /// </summary>
    /// <param name="signalCount">Number of signals.</param>
    /// <returns><see langword="true" /> if signal caused the <see cref="CurrentCount" /> to reach zero and the event was set, otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="signalCount" /> is less than 1.</exception>
    /// <exception cref="InvalidOperationException">If <paramref name="signalCount" /> is greater than current <see cref="CurrentCount" /> or event was already set.</exception>
    /// <remarks>Method is thread-safe.</remarks>
    public bool Signal(int signalCount)
    {
        Verify.ThrowIfLessOrEqual(signalCount, 0);

        var sw = new SpinWait();

        while (true)
        {
            var current = currentCount;

            if (signalCount > current) ThrowDecrementBelowZero();

            if (Interlocked.CompareExchange(ref currentCount, current - signalCount, current) == current)
            {
                if (current != signalCount) return false;
                completionSource.TrySetResult();
                return true;
            }

            sw.SpinOnce(-1);
        }
    }

    public Task WaitAsync(CancellationToken cancellationToken = default) =>
        cancellationToken == default
            ? completionSource.Task
            : completionSource.Task.WaitAsync(cancellationToken);

    public void Reset() => throw new NotImplementedException();

    public void Reset(int signalCount) => throw new NotImplementedException();

    [DoesNotReturn]
    private static void ThrowDecrementBelowZero() => throw new InvalidOperationException("Invalid attempt made to decrement the event's count below zero.");
}