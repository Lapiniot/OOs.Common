using System.Collections.Concurrent;

namespace System;

public class ObserversContainer<T> : IObservable<T>, IDisposable
{
    private ConcurrentDictionary<IObserver<T>, Subscription> observers;

    public ObserversContainer()
    {
        observers = new ConcurrentDictionary<IObserver<T>, Subscription>();
    }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        return observers switch
        {
            null => throw new InvalidOperationException("Container doesn't support subscription in the current state."),
            _ => observers.GetOrAdd(observer, o => new Subscription(o, this))
        };
    }

    private void Unsubscribe(IObserver<T> observer)
    {
        _ = (observers?.TryRemove(observer, out _));
    }

    public void Notify(T value)
    {
        _ = Parallel.ForEach(observers, (pair, _) =>
          {
              try
              {
                  pair.Key.OnNext(value);
              }
              catch
              {
                  // ignored
              }
          });
    }

    public void NotifyError(Exception error)
    {
        _ = Parallel.ForEach(observers, (pair, _) =>
          {
              try
              {
                  pair.Key.OnError(error);
              }
              catch
              {
                  // ignored
              }
          });
    }

    public void NotifyCompleted()
    {
        _ = Parallel.ForEach(observers, (pair, _) =>
          {
              try
              {
                  pair.Key.OnCompleted();
              }
              catch
              {
                  // ignored
              }
          });
    }

    private sealed class Subscription : IDisposable
    {
        private ObserversContainer<T> container;
        private IObserver<T> observer;

        public Subscription(IObserver<T> observer, ObserversContainer<T> container)
        {
            this.observer = observer;
            this.container = container;
        }

        public void Dispose()
        {
            container?.Unsubscribe(observer);
            container = null;
            observer = null;
        }
    }

    #region IDisposable Support

    private bool disposed;

    protected virtual void Dispose(bool disposing)
    {
        if(disposed) return;

        if(disposing)
        {
            var cached = Interlocked.Exchange(ref observers, null);

            if(cached != null)
            {
                _ = Parallel.ForEach(cached, (p, _) =>
                  {
                      try
                      {
                          p.Key.OnCompleted();
                      }
                      catch
                      {
                          // ignored
                      }
                  });

                cached.Clear();
            }
        }

        disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}