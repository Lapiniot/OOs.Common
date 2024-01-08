using System.Collections.Concurrent;

namespace OOs.Memory;

public sealed class ObjectPool<T> where T : class, new()
{
    private readonly ConcurrentQueue<T> bag;
    private int capacity;

    public ObjectPool(int capacity = 32)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        this.capacity = capacity;
        bag = new();
    }

#pragma warning disable CA1000 // Do not declare static members on generic types
    public static ObjectPool<T> Shared => InstanceHolder.Instance;
#pragma warning restore CA1000 // Do not declare static members on generic types

    public T Rent()
    {
        if (!bag.TryDequeue(out var value))
            return new();

        Interlocked.Increment(ref capacity);
        return value;
    }

    public void Return(T instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if (InterlockedExtensions.CompareDecrement(ref capacity, 0) is not 0)
            bag.Enqueue(instance);
    }

    private static class InstanceHolder
    {
        internal static readonly ObjectPool<T> Instance = new();
        static InstanceHolder() { }
    }
}