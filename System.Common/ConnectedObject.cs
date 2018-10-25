using System.Runtime.CompilerServices;

namespace System
{
    /// <summary>
    /// Provides base implementation for type whose instance represents some logical connection
    /// </summary>
    public abstract class ConnectedObject : IDisposable
    {
        private readonly object syncRoot = new object();
        private bool disposed;
        private bool isConnected;

        public bool IsConnected => isConnected;

        public void Connect()
        {
            CheckDisposed();

            if(!isConnected)
            {
                lock(syncRoot)
                {
                    if(!isConnected)
                    {
                        OnConnect();

                        isConnected = true;
                    }
                }
            }
        }

        public void Close()
        {
            if(isConnected)
            {
                lock(syncRoot)
                {
                    if(isConnected)
                    {
                        OnClose();

                        isConnected = false;
                    }
                }
            }
        }

        protected abstract void OnConnect();

        protected abstract void OnClose();

        protected void CheckConnected([CallerMemberName] string callerName = null)
        {
            if(!isConnected) throw new InvalidOperationException($"Cannot call '{callerName}' in disconnected state. Call \'Connect()\' before.");
        }

        protected void CheckDisposed()
        {
            if(disposed) throw new InvalidOperationException("Cannot use this instance - has been already disposed.");
        }

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if(!disposed)
            {
                if(disposing)
                {
                    Close();
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