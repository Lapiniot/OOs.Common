using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace System;

[SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "By design")]
public sealed class ObserversContainer<T> : IObservable<T>, IDisposable
{
    private ConcurrentDictionary<IObserver<T>, Subscription> observers;

    public ObserversContainer() => observers = new();

    private void Unsubscribe(IObserver<T> observer) => observers?.TryRemove(observer, out _);

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

#pragma warning disable CA1034 // Nested types should not be visible
    public readonly record struct Subscription : IDisposable
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
#pragma warning restore CA1034

    #region Implementation of IObservable<out T>

    IDisposable IObservable<T>.Subscribe(IObserver<T> observer) => Subscribe(observer);

    public Subscription Subscribe(IObserver<T> observer)
    {
        Verify.ThrowIfObjectDisposed(observers is null, nameof(ObserversContainer<T>));

        return observers!.GetOrAdd(observer, static (observer, container) => new(observer, container), this);
    }

    #endregion
}