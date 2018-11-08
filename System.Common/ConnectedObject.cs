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

        public bool IsConnected { get; private set; }

        public void Connect()
        {
            CheckDisposed();

            if(!IsConnected)
            {
                lock(syncRoot)
                {
                    if(!IsConnected)
                    {
                        OnConnect();

                        IsConnected = true;
                    }
                }
            }
        }

        public void Close()
        {
            if(IsConnected)
            {
                lock(syncRoot)
                {
                    if(IsConnected)
                    {
                        OnClose();

                        IsConnected = false;
                    }
                }
            }
        }

        protected abstract void OnConnect();

        protected abstract void OnClose();

        protected void CheckConnected([CallerMemberName] string callerName = null)
        {
            if(!IsConnected) throw new InvalidOperationException($"Cannot call '{callerName}' in disconnected state. Call \'Connect()\' before.");
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