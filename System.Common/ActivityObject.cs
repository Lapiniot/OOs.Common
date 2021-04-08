using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System
{
    /// <summary>
    /// Implements async state management template for any type which runs some activity and may be in two states only
    /// (Running/Stopped, Connected/Disconnected e.g.).
    /// </summary>
    public abstract class ActivityObject : IAsyncDisposable
    {
        private readonly SemaphoreSlim semaphore = new(1);
        private bool disposed;
        private long disposeState;

        protected bool IsRunning { get; private set; }

        #region Implementation of IAsyncDisposable

        public virtual async ValueTask DisposeAsync()
        {
            if(Interlocked.CompareExchange(ref disposeState, 1, 0) != 0) return;

            try
            {
                await StopActivityAsync().ConfigureAwait(false);
            }
            finally
            {
                semaphore.Dispose();
                disposed = true;
            }
        }

        #endregion

        protected abstract Task StartingAsync(CancellationToken cancellationToken);

        protected abstract Task StoppingAsync();

        protected void CheckState(bool state, [CallerMemberName] string callerName = null)
        {
            if(IsRunning != state) throw new InvalidOperationException($"Cannot call '{callerName}' in the current state.");
        }

        protected void CheckDisposed()
        {
            if(disposed) throw new InvalidOperationException("Cannot use this instance - has been already disposed.");
        }

        protected async Task StartActivityAsync(CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            if(!IsRunning)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    if(!IsRunning)
                    {
                        await StartingAsync(cancellationToken).ConfigureAwait(false);

                        IsRunning = true;
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }

        protected async Task StopActivityAsync()
        {
            CheckDisposed();

            if(IsRunning)
            {
                await semaphore.WaitAsync().ConfigureAwait(false);

                try
                {
                    if(IsRunning)
                    {
                        await StoppingAsync().ConfigureAwait(false);
                    }
                }
                finally
                {
                    IsRunning = false;
                    semaphore.Release();
                }
            }
        }
    }
}