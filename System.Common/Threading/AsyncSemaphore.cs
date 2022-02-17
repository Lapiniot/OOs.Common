using System.Runtime.CompilerServices;
using static System.Properties.Strings;
using static System.Threading.Tasks.TaskCreationOptions;

namespace System.Threading;

public class AsyncSemaphore
{
    private readonly int maxCount;
    private readonly object syncRoot;
    private int currentCount;
    private int waitersCount;
    private Node head;
    private Node tail;

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

            var node = new Node();

            if(head is null)
            {
                head = tail = node;
            }
            else
            {
                tail = tail.Next = node;
            }

            return new ValueTask(node.CompletionSource.Task.WaitAsync(cancellationToken));
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

            while(waitersCount > 0 && releaseCount > 0 && head is not null)
            {
                head.CompletionSource.SetResult();
                head = head.Next;
                waitersCount--;
                releaseCount--;
            }

            currentCount += releaseCount;
        }
    }

    private class Node
    {
        public Node()
        {
            CompletionSource = new TaskCompletionSource(RunContinuationsAsynchronously);
        }

        public TaskCompletionSource CompletionSource { get; }
        public Node Next { get; set; }
    }
}