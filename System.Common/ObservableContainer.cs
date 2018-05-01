using System.Collections.Concurrent;
using System.Threading;

namespace System.Common
{
    public class ObservableContainer<T> : IObservable<T>, IDisposable
    {
        private ConcurrentDictionary<IObserver<T>, Subscription> observers;

        public ObservableContainer()
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
            observers?.TryRemove(observer, out var _);
        }

        public void Notify(T value)
        {
            foreach(var pair in observers) pair.Key.OnNext(value);
        }

        private class Subscription : IDisposable
        {
            private IObserver<T> observer;
            private ObservableContainer<T> container;

            public Subscription(IObserver<T> observer, ObservableContainer<T> container)
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
                        foreach(var pair in cached) pair.Key.OnCompleted();

                        cached.Clear();
                    }
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}