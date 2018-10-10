using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    public abstract class ConnectionListener : IConnectionListener
    {
        private readonly object syncRoot = new object();
        private bool isListening;

        public bool IsListening => isListening;

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

        public abstract Task<INetworkTransport> AcceptAsync(CancellationToken cancellationToken);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void OnStartListening();

        protected abstract void OnStopListening();

        protected virtual void Dispose(bool disposing)
        {
            if(disposing)
            {
            }
        }
    }
}