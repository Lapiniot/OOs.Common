using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System
{
    [Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Method implements IAsyncDisposable instead")]
    public abstract class ConnectedObject : IConnectedObject, IAsyncDisposable
    {
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);

        protected abstract Task OnConnectAsync(CancellationToken cancellationToken);

        protected abstract Task OnDisconnectAsync();

        protected void CheckConnected([CallerMemberName] string callerName = null)
        {
            if(!IsConnected) throw new InvalidOperationException($"Cannot call '{callerName}' in disconnected state.");
        }

        protected void CheckDisposed()
        {
            if(disposed) throw new InvalidOperationException("Cannot use this instance - has been already disposed.");
        }

        #region Implementation of IAsyncDisposable

        private bool disposed;

        public virtual async ValueTask DisposeAsync()
        {
            if(!disposed)
            {
                try
                {
                    await DisconnectAsync().ConfigureAwait(false);
                }
                finally
                {
                    semaphore.Dispose();
                    disposed = true;
                }
            }
        }

        #endregion

        #region Implementation of IConnectedObject

        public bool IsConnected { get; private set; }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            if(!IsConnected)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    if(!IsConnected)
                    {
                        await OnConnectAsync(cancellationToken).ConfigureAwait(false);

                        IsConnected = true;
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

            if(IsConnected)
            {
                await semaphore.WaitAsync().ConfigureAwait(false);

                try
                {
                    if(IsConnected)
                    {
                        await OnDisconnectAsync().ConfigureAwait(false);
                    }
                }
                finally
                {
                    IsConnected = false;
                    semaphore.Release();
                }
            }
        }

        #endregion
    }
}