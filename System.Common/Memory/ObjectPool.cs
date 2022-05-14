using System.Collections.Concurrent;

namespace System.Memory;

public sealed class ObjectPool<T> where T : class, new()
{
    private readonly ConcurrentQueue<T> bag;
    private int capacity;

    public ObjectPool(int capacity = 32)
    {
        Verify.ThrowIfLessOrEqual(capacity, 0);
        this.capacity = capacity;
        bag = new();
    }

#pragma warning disable CA1000

    public static ObjectPool<T> Shared => InstanceHolder.instance;

#pragma warning restore CA1000

    public T Rent()
    {
        if (!bag.TryDequeue(out var value))
        {
            return new();
        }

        Interlocked.Increment(ref capacity);
        return value;
    }

    public void Return(T instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if (InterlockedExtensions.CompareDecrement(ref capacity, 0) is not 0)
        {
            bag.Enqueue(instance);
        }
    }

    private static class InstanceHolder
    {
        internal static readonly ObjectPool<T> instance = new();
        static InstanceHolder() { }
    }
}