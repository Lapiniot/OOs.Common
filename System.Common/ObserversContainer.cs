using System.Collections.Concurrent;

namespace System;

#pragma warning disable CA1031

public sealed class ObserversContainer<T> : IObservable<T>, IDisposable
{
    private ConcurrentDictionary<IObserver<T>, Subscription<T>> observers;

    public ObserversContainer() => observers = new();

    internal void Unsubscribe(IObserver<T> observer) => observers?.TryRemove(observer, out _);

    public void Notify(T value)
    {
        foreach (var (observer, _) in observers)
        {
            try
            {
                observer.OnNext(value);
            }
            catch
            {
                // ignored
            }
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
            catch
            {
                // ignored
            }
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
            catch
            {
                // ignored
            }
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
            catch
            {
                // ignored
            }
        }

        current.Clear();
    }

    #endregion

    #region Implementation of IObservable<out T>

    IDisposable IObservable<T>.Subscribe(IObserver<T> observer) => Subscribe(observer);

    public Subscription<T> Subscribe(IObserver<T> observer)
    {
        Verify.ThrowIfObjectDisposed(observers is null, nameof(ObserversContainer<T>));

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