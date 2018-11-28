using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    public abstract class ConnectionListener : IConnectionListener
    {
        private readonly object syncRoot = new object();

        public bool IsListening { get; private set; }

        public void Start()
        {
            if(!IsListening)
            {
                lock(syncRoot)
                {
                    if(!IsListening)
                    {
                        OnStartListening();
                        IsListening = true;
                    }
                }
            }
        }

        public void Stop()
        {
            if(IsListening)
            {
                lock(syncRoot)
                {
                    if(IsListening)
                    {
                        OnStopListening();
                        IsListening = false;
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
            if(disposing) {}
        }
    }
}