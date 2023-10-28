using System.Collections.Concurrent;

namespace System.Memory;

public sealed class GenericPool<T>
{
    private readonly ConcurrentBag<T> bag = [];
    private readonly Func<T> factory;
    private int capacity;

    public GenericPool(Func<T> factory, int capacity = 32)
    {
        Verify.ThrowIfLessOrEqual(capacity, 0);
        this.factory = factory;
        this.capacity = capacity;
    }

    public T Rent()
    {
        if (!bag.TryTake(out var value))
        {
            return factory();
        }

        Interlocked.Increment(ref capacity);
        return value;
    }

    public void Return(T instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if (InterlockedExtensions.CompareDecrement(ref capacity, 0) is not 0)
        {
            bag.Add(instance);
        }
    }
}