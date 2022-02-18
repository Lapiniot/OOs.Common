using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace System;

public sealed class ObserversContainer<T> : IObservable<T>, IDisposable
{
    private bool disposed;
    private ConcurrentDictionary<IObserver<T>, Subscription> observers;

    public ObserversContainer() => observers = new();

    private void Unsubscribe(IObserver<T> observer) => observers?.TryRemove(observer, out _);

    [SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "By design")]
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

    [SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "By design")]
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

    [SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "By design")]
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

    [SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "By design")]
    public void Dispose()
    {
        if (disposed) return;

        var cached = Interlocked.Exchange(ref observers, null);

        if (cached != null)
        {
            foreach (var (observer, _) in cached)
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

            cached.Clear();
        }

        disposed = true;
    }

    #endregion

    #region Implementation of IObservable<out T>

    IDisposable IObservable<T>.Subscribe(IObserver<T> observer) => Subscribe(observer);

    public Subscription Subscribe(IObserver<T> observer) =>
        observers switch
        {
            null => throw new ObjectDisposedException("Container doesn't support subscription in the current state."),
            _ => observers.GetOrAdd(observer, static (o, c) => new(o, c), this)
        };

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
}