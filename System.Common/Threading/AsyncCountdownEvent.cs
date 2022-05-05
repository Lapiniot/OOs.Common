#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace System.Threading;

public class AsyncCountdownEvent
{
    private readonly TaskCompletionSource completionSource;
    private readonly int initialCount;
    private  int currentCount;

    public AsyncCountdownEvent(int signalCount)
    {
        Verify.ThrowIfLess(signalCount, 0);
        currentCount = initialCount = signalCount;
        completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        if (currentCount is 0) completionSource.TrySetResult();
    }

    public int CurrentCount => currentCount;

    public int InitialCount => initialCount;

    public void AddCount() => AddCount(1);

    public void AddCount(int signalCount) => throw new NotImplementedException();

    public void Signal() => Signal(1);

    public bool Signal(int signalCount)
    {
        Verify.ThrowIfLessOrEqual(signalCount, 0);

        var sw = new SpinWait();

        while (true)
        {
            var current = currentCount;

            if (current < signalCount) ThrowDecrementBelowZero();

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