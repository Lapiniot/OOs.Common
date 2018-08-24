using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System
{
    public abstract class AsyncConnectedObject<TOptions> : IDisposable
    {
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);

        public bool Connected
        {
            get { return connected; }
        }

        public async Task ConnectAsync(TOptions options, CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            if(!connected)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    if(!connected)
                    {
                        await OnConnectAsync(options, cancellationToken).ConfigureAwait(false);

                        connected = true;
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }

        public async Task CloseAsync()
        {
            CheckDisposed();

            if(connected)
            {
                await semaphore.WaitAsync().ConfigureAwait(false);

                try
                {
                    if(connected)
                    {
                        await OnCloseAsync().ConfigureAwait(false);

                        connected = false;
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }

        protected abstract Task OnConnectAsync(TOptions options, CancellationToken cancellationToken);

        protected abstract Task OnCloseAsync();

        protected void CheckConnected([CallerMemberName] string callerName = null)
        {
            if(!connected) throw new InvalidOperationException($"Cannot call '{callerName}' in disconnected state. Call \'Connect()\' before.");
        }

        protected void CheckDisposed()
        {
            if(disposed) throw new InvalidOperationException("Cannot use this instance - has been already disposed.");
        }

        #region IDisposable Support

        private bool disposed;

        private bool connected;

        protected virtual void Dispose(bool disposing)
        {
            if(!disposed)
            {
                if(disposing)
                {
                    CloseAsync().ContinueWith(t => { semaphore.Dispose(); });
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