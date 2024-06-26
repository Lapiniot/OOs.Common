using System.Collections.Concurrent;

namespace OOs;

public sealed class ObserversContainer<T> : IObservable<T>, IDisposable
{
    private ConcurrentDictionary<IObserver<T>, Subscription<T>> observers;

    public ObserversContainer() => observers = new();

    internal void Unsubscribe(IObserver<T> observer) => observers?.TryRemove(observer, out _);

    public void Notify(in T value)
    {
        foreach (var (observer, _) in observers)
        {
            try
            {
                observer.OnNext(value);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch { /* by design */}
#pragma warning restore CA1031 // Do not catch general exception types
        }
    }

    public void NotifyError(Exception error)
    {
        foreach (var (observer, _) in observers)
        {
            try
            {
                observer.OnError(error);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch { /* by design */ }
#pragma warning restore CA1031 // Do not catch general exception types
        }
    }

    public void NotifyCompleted()
    {
        foreach (var (observer, _) in observers)
        {
            try
            {
                observer.OnCompleted();
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch { /* by design */ }
#pragma warning restore CA1031 // Do not catch general exception types
        }
    }

    #region IDisposable Support

    public void Dispose()
    {
        var current = Interlocked.Exchange(ref observers, null);

        if (current is null) return;

        foreach (var (observer, _) in current)
        {
            try
            {
                observer.OnCompleted();
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch { /* by design */ }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        current.Clear();
    }

    #endregion

    #region Implementation of IObservable<out T>

    IDisposable IObservable<T>.Subscribe(IObserver<T> observer) => Subscribe(observer);

    public Subscription<T> Subscribe(IObserver<T> observer)
    {
        ObjectDisposedException.ThrowIf(observers is null, this);
        return observers!.GetOrAdd(observer, static (observer, container) => new(observer, container), this);
    }

    #endregion
}

public readonly record struct Subscription<T> : IDisposable
{
    private readonly ObserversContainer<T> container;
    private readonly IObserver<T> observer;

    internal Subscription(IObserver<T> observer, ObserversContainer<T> container)
    {
        this.observer = observer;
        this.container = container;
    }

    public void Dispose() => container.Unsubscribe(observer);
}