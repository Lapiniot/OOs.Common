﻿using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace OOs.Threading;

/// <summary>
/// Represents synchronization primitive that is signaled when its counter reaches zero. 
/// </summary>
/// <remarks>It is effectively <seealso cref="CountdownEvent"/> analog optimized for async. programming.</remarks>
public sealed class AsyncCountdownEvent
{
    private TaskCompletionSource completionSource;
    private int initialCount;
    private int currentCount;

    /// <summary>
    /// Initializes new instance of <see cref="AsyncCountdownEvent"/>.
    /// </summary>
    /// <param name="signalCount">The number of signals initially required to set the event.</param>
    /// <exception cref="ArgumentOutOfRangeException">When the value of <paramref name="signalCount"/> is negative.</exception>
    public AsyncCountdownEvent(int signalCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(signalCount);
        currentCount = initialCount = signalCount;
        completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        if (currentCount is 0)
            completionSource.SetResult();
    }

    /// <summary>
    /// Gets the number of remaining signals required to set the event.
    /// </summary>
    /// <value>The number of remaining signals required to set the event.</value>
    public int CurrentCount
    {
        get
        {
            var current = Volatile.Read(ref currentCount);
            return current > 0 ? current : 0;
        }
    }

    /// <summary>
    /// Gets the number of signals initially required to set the event.
    /// </summary>
    /// <value>The number of signals initially required to set the event.</value>
    public int InitialCount => initialCount;

    /// <summary>
    /// Increments <see cref="CurrentCount" /> by one.
    /// </summary>
    /// <exception cref="InvalidOperationException">When the event is already signaled and cannot be incremented.</exception>
    /// <exception cref="InvalidOperationException">The increment operation would cause the <see cref="CurrentCount"/> to overflow.</exception>
    /// <remarks>Method is thread-safe.</remarks>
    public void AddCount() => AddCount(1);

    /// <summary>
    /// Increments <see cref="CurrentCount" /> by a <paramref name="signalCount" /> value.
    /// </summary>
    /// <param name="signalCount">The value increment.</param>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="signalCount"/> is less or equal 0.</exception>
    /// <exception cref="InvalidOperationException">When the event is already signaled and cannot be incremented.</exception>
    /// <exception cref="InvalidOperationException">The increment operation would cause the <see cref="CurrentCount"/> to overflow.</exception>
    /// <remarks>Method is thread-safe.</remarks>
    public void AddCount(int signalCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(signalCount, 0);

        var sw = new SpinWait();

        while (true)
        {
            var current = currentCount;

            if (current <= 0)
                ThrowEventAlreadySet();

            if (current > int.MaxValue - signalCount)
                ThrowIncrementAlreadyMax();

            if (Interlocked.CompareExchange(ref currentCount, current + signalCount, current) == current)
                break;

            sw.SpinOnce(-1);
        }
    }

    /// <summary>
    /// Signals current <see cref="AsyncCountdownEvent" /> instance, decrementing <see cref="CurrentCount" /> by one.
    /// </summary>
    /// <returns><see langword="true" /> if signal caused the <see cref="CurrentCount" /> to reach zero and 
    /// the event was set, otherwise <see langword="false" />.</returns>
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
    /// Signals current <see cref="AsyncCountdownEvent" /> instance, decrementing <see cref="CurrentCount" /> 
    /// by specified <paramref name="signalCount" /> value.
    /// </summary>
    /// <param name="signalCount">Number of signals.</param>
    /// <returns><see langword="true" /> if signal caused the <see cref="CurrentCount" /> to reach zero and the event was set, 
    /// otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="signalCount" /> is less than 1.</exception>
    /// <exception cref="InvalidOperationException">If <paramref name="signalCount" /> is greater than current <see cref="CurrentCount" /> 
    /// or event was already set.</exception>
    /// <remarks>Method is thread-safe.</remarks>
    public bool Signal(int signalCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(signalCount, 0);

        var sw = new SpinWait();

        while (true)
        {
            var current = currentCount;

            if (signalCount > current)
                ThrowDecrementBelowZero();

            if (Interlocked.CompareExchange(ref currentCount, current - signalCount, current) == current)
            {
                if (current != signalCount) return false;
                completionSource.TrySetResult();
                return true;
            }

            sw.SpinOnce(-1);
        }
    }

    /// <summary>
    /// Asynchronously waits until <see cref="CurrentCount"/> reaches zero so event is signaled.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task which is completed when <see cref="CurrentCount"/> reaches zero.</returns>
    public Task WaitAsync(CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled(cancellationToken)
            : completionSource.Task;

    /// <summary>
    /// Resets the <see cref="CurrentCount"/> to a specified <see cref="InitialCount"/> value.
    /// </summary>
    /// <remarks>Attention: method is not thread-safe.</remarks>
    public void Reset() => Reset(initialCount);

    /// <summary>
    /// Resets the <see cref="CurrentCount"/> to a specified <paramref name="signalCount"/> value.
    /// </summary>
    /// <param name="signalCount">The number of signals required to set the event.</param>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="signalCount"/> is negative.</exception>
    /// <remarks>Attention: method is not thread-safe.</remarks>
    public void Reset(int signalCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(signalCount);

        currentCount = initialCount = signalCount;

        if (completionSource.Task.IsCompleted)
            completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        if (signalCount == 0)
            completionSource.SetResult();
    }

    [DoesNotReturn]
    private static void ThrowDecrementBelowZero() =>
        throw new InvalidOperationException("Invalid attempt made to decrement the event's count below zero.");

    [DoesNotReturn]
    private static void ThrowEventAlreadySet() =>
        throw new InvalidOperationException("The event is already signaled and cannot be incremented.");

    [DoesNotReturn]
    private static void ThrowIncrementAlreadyMax() =>
        throw new InvalidOperationException("The increment operation would cause the CurrentCount to overflow.");
}