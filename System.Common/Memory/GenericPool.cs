using System.Collections.Concurrent;

namespace System.Memory;

public sealed class GenericPool<T>
{
    private readonly ConcurrentBag<T> bag = new();
    private int capacity;
    private readonly Func<T> factory;

    public GenericPool(Func<T> factory, int capacity = 32)
    {
        if(capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        this.factory = factory;
        this.capacity = capacity;
    }

    public T Rent()
    {
        if(!bag.TryTake(out var value))
        {
            return factory();
        }

        Interlocked.Increment(ref capacity);
        return value;
    }

    public void Return(T instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if(InterlockedExtensions.CompareDecrement(ref capacity, 0) is not 0)
        {
            bag.Add(instance);
        }
    }
}