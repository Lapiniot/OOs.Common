using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace System
{
    public class ObserversContainer<T> : IObservable<T>, IDisposable
    {
        private ConcurrentDictionary<IObserver<T>, Subscription> observers;

        public ObserversContainer()
        {
            observers = new ConcurrentDictionary<IObserver<T>, Subscription>();
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return observers?.GetOrAdd(observer, o => new Subscription(o, this)) ??
                   throw new InvalidOperationException("Container doesn't support subscription in current state.");
        }

        private void Unsubscribe(IObserver<T> observer)
        {
            observers?.TryRemove(observer, out _);
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "None of the handler exceptions should break notification loop")]
        public void Notify(T value)
        {
            Parallel.ForEach(observers, (pair, state) =>
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

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "None of the handler exceptions should break notification loop")]
        public void NotifyError(Exception error)
        {
            Parallel.ForEach(observers, (pair, state) =>
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

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "None of the handler exceptions should break notification loop")]
        public void NotifyCompleted()
        {
            Parallel.ForEach(observers, (pair, state) =>
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
            if(!disposed)
            {
                if(disposing)
                {
                    var cached = Interlocked.Exchange(ref observers, null);

                    if(cached != null)
                    {
                        foreach(var pair in cached)
                        {
                            pair.Key.OnCompleted();
                        }

                        cached.Clear();
                    }
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}