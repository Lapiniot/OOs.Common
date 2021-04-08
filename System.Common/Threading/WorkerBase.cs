using System.Threading.Tasks;

namespace System.Threading
{
    /// <summary>
    /// Base class for types that run some work asynchronously on the background
    /// </summary>
    public abstract class WorkerBase : IAsyncDisposable
    {
        private readonly SemaphoreSlim semaphore = new(1);
        private CancelableOperationScope cancelableOperation;
        private int disposed;

        /// <summary>
        /// Must be implemented by derived type and represents actual asynchronous operation to be run on background
        /// </summary>
        /// <param name="stoppingToken"><see cref="CancellationToken" /> for cancellation signaling</param>
        /// <returns>Awaitable task, representing actual background work</returns>
        protected abstract Task ExecuteAsync(CancellationToken stoppingToken);

        /// <summary>
        /// Starts work and returns task that represent running asynchronous work
        /// </summary>
        /// <param name="stoppingToken"><see cref="CancellationToken" /> that signals about external cancellation</param>
        /// <returns>Awaitable task that represents currently running operation</returns>
        public async Task RunAsync(CancellationToken stoppingToken)
        {
            await semaphore.WaitAsync(stoppingToken).ConfigureAwait(false);

            CancelableOperationScope captured;

            try
            {
                captured = cancelableOperation ??= CancelableOperationScope.StartInScope(ct => ExecuteAsync(ct), stoppingToken);
            }
            finally
            {
                semaphore.Release();
            }

            await captured.Completion.ConfigureAwait(false);
        }

        /// <summary>
        /// Signals currently running asynchronous work about completion request
        /// </summary>
        /// <returns>Awaitable task which represents result of background work completion</returns>
        public async Task StopAsync()
        {
            await semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                await using(cancelableOperation)
                {
                    cancelableOperation = null;
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        #region Implementation of IAsyncDisposable

        public bool IsRunning => Volatile.Read(ref cancelableOperation) != null;

        public virtual async ValueTask DisposeAsync()
        {
            if(Interlocked.CompareExchange(ref disposed, 1, 0) != 0) return;

            try
            {
                await StopAsync().ConfigureAwait(false);
            }
            finally
            {
                semaphore.Dispose();
            }
        }

        #endregion
    }
}