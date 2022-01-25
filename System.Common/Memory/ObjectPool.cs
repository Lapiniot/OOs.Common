using System.Collections.Concurrent;

namespace System.Memory;

public sealed class ObjectPool<T> where T : class, new()
{
    private readonly ConcurrentBag<T> bag = new();
    private int capacity;

    public ObjectPool(int capacity = 32)
    {
        if(capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        this.capacity = capacity;
    }

#pragma warning disable CA1000

    public static ObjectPool<T> Shared => InstanceHolder.instance;

#pragma warning restore CA1000

    public T Rent()
    {
        if(bag.TryTake(out var value))
        {
            Interlocked.Increment(ref capacity);
            return value;
        }
        else
        {
            return new T();
        }
    }

    public void Return(T instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if(CompareDecrement(ref capacity, 0) is not 0)
        {
            bag.Add(instance);
        }
    }

    private static int CompareDecrement(ref int location, int minComparand)
    {
        int current;

        do
        {
            current = Volatile.Read(ref location);
            if(current - 1 < minComparand) return current;
        } while(Interlocked.CompareExchange(ref location, current - 1, current) != current);

        return current;
    }

    private class InstanceHolder
    {
        static InstanceHolder() { }
        internal static readonly ObjectPool<T> instance = new();
    }
}
