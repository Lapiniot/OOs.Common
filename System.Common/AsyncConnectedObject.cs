using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System
{
    public abstract class AsyncConnectedObject : IAsyncConnectedObject
    {
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);

        public bool Connected
        {
            get { return connected; }
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            if(!connected)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    if(!connected)
                    {
                        await OnConnectAsync(cancellationToken).ConfigureAwait(false);

                        connected = true;

                        await OnConnectedAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }

        public async Task DisconnectAsync()
        {
            CheckDisposed();

            if(connected)
            {
                await semaphore.WaitAsync().ConfigureAwait(false);

                try
                {
                    if(connected)
                    {
                        await OnDisconnectAsync().ConfigureAwait(false);
                    }
                }
                finally
                {
                    connected = false;
                    semaphore.Release();
                }
            }
        }

        protected abstract Task OnConnectAsync(CancellationToken cancellationToken);

        protected abstract Task OnConnectedAsync(CancellationToken cancellationToken);

        protected abstract Task OnDisconnectAsync();

        protected void CheckConnected([CallerMemberName] string callerName = null)
        {
            if(!connected) throw new InvalidOperationException($"Cannot call '{callerName}' in disconnected state.");
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
                    DisconnectAsync().ContinueWith(t => { semaphore.Dispose(); });
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