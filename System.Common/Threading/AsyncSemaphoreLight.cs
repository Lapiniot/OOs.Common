using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Tasks.Sources;

#nullable enable

namespace System.Threading;

/// <summary>
/// Provides simplified semaphore with asynchronous waiting behavior. This implementation is only suitable for use-cases with a single 
/// consumer calling <see cref="WaitAsync(CancellationToken)"/>. At the same time any arbitrary thread may call <see cref="Release"/> or 
/// <see cref="TryRelease"/> to increment semaphore counter and potentially unblock pending waiter.
/// </summary>
/// <remarks>
/// Do not use this implementation for other scenarious where concurrent calls to <see cref="WaitAsync(CancellationToken)"/> are potentially possible.
/// </remarks>
public sealed class AsyncSemaphoreLight : IValueTaskSource, IProvideInstrumentationMetrics
{
    private static long waitingCount;
    private readonly int maxCount;
    private readonly object syncRoot;
    private int currentCount;
    private ManualResetValueTaskSourceCore<int> mrvtsc;
    private CancellationTokenRegistration ctr;
    private bool waiting;

    public AsyncSemaphoreLight(int initialCount, int maxCount = int.MaxValue, bool runContinuationsAsynchronously = true)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxCount, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCount, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialCount, maxCount);

        this.maxCount = maxCount;
        currentCount = initialCount;
        mrvtsc = new() { RunContinuationsAsynchronously = runContinuationsAsynchronously };
        syncRoot = new();
    }

    public int CurrentCount => currentCount is >= 0 and var current ? current : 0;

    public int MaxCount => maxCount;

    #region IValueTaskSource implementation

    void IValueTaskSource.GetResult(short token) => mrvtsc.GetResult(token);
    ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) => mrvtsc.GetStatus(token);
    void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
        mrvtsc.OnCompleted(continuation, state, token, flags);

    #endregion

    public bool TryRelease()
    {
        lock (syncRoot)
        {
            var current = currentCount;

            if (current + 1 > maxCount)
                return false;

            currentCount++;

            if (current >= 0)
                return true;

            ctr.Unregister();
            mrvtsc.SetResult(0);
            waiting = false;
        }

        return true;
    }

    public void Release()
    {
        if (TryRelease()) return;
        ThrowHelper.ThrowSemaphoreFull();
    }

    private void Cancel(CancellationToken cancellationToken)
    {
        lock (syncRoot)
        {
            if (!waiting) return;

            currentCount++;
            mrvtsc.SetException(new OperationCanceledException(cancellationToken));
            waiting = false;
        }
    }

    public ValueTask WaitAsync(CancellationToken cancellationToken)
    {
        lock (syncRoot)
        {
            if (waiting) ThrowHelper.ThrowInvalidOperation();
            return cancellationToken.IsCancellationRequested
                ? ValueTask.FromCanceled(cancellationToken)
                : --currentCount >= 0 ? ValueTask.CompletedTask : WaitCoreAsync(cancellationToken);
        }

        ValueTask WaitCoreAsync(CancellationToken cancellationToken)
        {
            if (RuntimeSettings.ThreadingInstrumentationSupport)
            {
                Interlocked.Increment(ref waitingCount);
            }

            waiting = true;
            mrvtsc.Reset();
            if (cancellationToken.CanBeCanceled)
            {
                ctr = cancellationToken.UnsafeRegister(CancelCallback, this);
            }

            return new ValueTask(this, mrvtsc.Version);

            static void CancelCallback(object? state, CancellationToken token) => ((AsyncSemaphoreLight)state!).Cancel(token);
        }
    }

    public static IDisposable? EnableInstrumentation(string? meterName = null)
    {
        if (RuntimeSettings.ThreadingInstrumentationSupport)
        {
            var meter = new Meter(meterName ?? "System.Threading.AsyncSemaphoreLight");
            meter.CreateObservableGauge("waiting-count", static () => waitingCount, description: "Number of asynchronous waits");
            return meter;
        }

        return null;
    }
}