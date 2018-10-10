namespace System.Net
{
    public abstract class ConnectionListener : IObservable<INetworkTransport>, IDisposable
    {
        private readonly ObserversContainer<INetworkTransport> observers = new ObserversContainer<INetworkTransport>();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IDisposable Subscribe(IObserver<INetworkTransport> observer)
        {
            return observers.Subscribe(observer);
        }

        public abstract void Start();

        public abstract void Stop();

        protected virtual void Dispose(bool disposing)
        {
            if(disposing)
            {
                observers?.Dispose();
            }
        }
    }
}