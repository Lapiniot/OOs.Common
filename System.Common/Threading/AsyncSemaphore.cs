using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks.Sources;
using static System.Properties.Strings;

namespace System.Threading;

#nullable enable

public class AsyncSemaphore
{
    private readonly int maxCount;
    private readonly object syncRoot;
    private int currentCount;
    private WaiterNode? head;
    private WaiterNode? tail;
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
        syncRoot = new object();
    }

    public int MaxCount => maxCount;

    public int CurrentCount => Volatile.Read(ref currentCount);

    public ValueTask WaitAsync(CancellationToken cancellationToken = default)
    {
        if(cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled(cancellationToken);
        }

        lock(syncRoot)
        {
            if(currentCount > 0)
            {
                currentCount--;
                return ValueTask.CompletedTask;
            }

            waitersCount++;

            var node = new WaiterNode();
            node.Bind(cancellationToken);

            if(head is null)
            {
                head = tail = node;
            }
            else
            {
                tail = tail!.Next = node;
            }

            return new ValueTask(node, node.Version);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Release() => Release(1);

    public void Release(int releaseCount)
    {
        lock(syncRoot)
        {
            if(currentCount + releaseCount - waitersCount > maxCount)
            {
                throw new SemaphoreFullException();
            }

            while(waitersCount > 0 && releaseCount > 0 && head is not null)
            {
                if(head.TrySetComplete()) releaseCount--;
                head = head.Next;
                waitersCount--;
            }

            currentCount += releaseCount;
        }
    }

    private class WaiterNode : IValueTaskSource
    {
        private int completed;
        private ManualResetValueTaskSourceCore<bool> core;
        private CancellationTokenRegistration? registration;

        public WaiterNode()
        {
            core = new ManualResetValueTaskSourceCore<bool> { RunContinuationsAsynchronously = true };
        }

        public short Version => core.Version;

        public WaiterNode? Next { get; set; }

        public void Reset()
        {
            ResetPendingCancellation();
            core.Reset();
            completed = 0;
        }

        public void Bind(CancellationToken cancellationToken)
        {
            ResetPendingCancellation();

            if(cancellationToken != default)
            {
                registration = cancellationToken.UnsafeRegister(
                    static (state, token) => ((WaiterNode)state!).TrySetCanceled(token),
                    this);
            }
        }

        public bool TrySetComplete()
        {
            if(Interlocked.CompareExchange(ref completed, 1, 0) == 0)
            {
                ResetPendingCancellation();
                core.SetResult(true);
                return true;
            }

            return false;
        }

        public bool TrySetCanceled(CancellationToken cancellationToken) =>
            TrySetException(ExceptionDispatchInfo.SetCurrentStackTrace(
                new OperationCanceledException(cancellationToken)));

        public bool TrySetException(Exception error)
        {
            if(Interlocked.CompareExchange(ref completed, 1, 0) == 0)
            {
                ResetPendingCancellation();
                core.SetException(error);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResetPendingCancellation()
        {
            var local = registration;
            registration = null;

            if(local.HasValue)
            {
                local.Value.Dispose();
            }
        }

        #region Implementation of IValueTaskSource

        public void GetResult(short token)
        {
            try
            {
                core.GetResult(token);
            }
            finally
            {
                Reset();
            }
        }

        public ValueTaskSourceStatus GetStatus(short token) => core.GetStatus(token);

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
            core.OnCompleted(continuation, state, token, flags);

        #endregion
    }
}