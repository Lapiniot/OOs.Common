using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    public abstract class ConnectionListener : IConnectionListener
    {
        private readonly ObserversContainer<INetworkTransport> observers = new ObserversContainer<INetworkTransport>();
        private readonly object syncRoot = new object();
        private CancellationTokenSource cancellationTokenSource;
        private bool isListening;

        public bool IsListening => isListening;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IDisposable Subscribe(IObserver<INetworkTransport> observer)
        {
            return observers.Subscribe(observer);
        }

        public void Start()
        {
            if(!isListening)
            {
                lock(syncRoot)
                {
                    if(!isListening)
                    {
                        OnStartListening();
                        isListening = true;
                        cancellationTokenSource = new CancellationTokenSource();
                        Task.Run(StartAcceptingConnectionsAsync);
                    }
                }
            }
        }

        public void Stop()
        {
            if(isListening)
            {
                lock(syncRoot)
                {
                    if(isListening)
                    {
                        OnStopListening();
                        isListening = false;
                    }
                }
            }
        }

        private async Task StartAcceptingConnectionsAsync()
        {
            var token = cancellationTokenSource.Token;

            while(!token.IsCancellationRequested)
            {
                var networkTransport = await AcceptAsync(token).ConfigureAwait(false);
                observers.Notify(networkTransport);
            }
        }

        protected abstract Task<INetworkTransport> AcceptAsync(CancellationToken cancellationToken);

        protected abstract void OnStartListening();

        protected abstract void OnStopListening();

        protected virtual void Dispose(bool disposing)
        {
            if(disposing)
            {
                cancellationTokenSource?.Dispose();
                observers?.Dispose();
            }
        }
    }
}