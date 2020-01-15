using System.Threading.Tasks;
using static System.Properties.Strings;

namespace System.Threading
{
    /// <summary>
    /// Base class for types that run some work asynchronously on the background
    /// </summary>
    public abstract class WorkerBase : IAsyncDisposable
    {
        private bool disposed;
        private CancellationTokenSource globalCts;
        private int stateSentinel;
        private Task worker;

        public bool IsRunning => Volatile.Read(ref stateSentinel) == 1;

        #region Implementation of IAsyncDisposable

        public virtual async ValueTask DisposeAsync()
        {
            if(disposed) return;

            try
            {
                await StopAsync().ConfigureAwait(false);
            }
            finally
            {
                disposed = true;
            }
        }

        #endregion

        /// <summary>
        /// Must be implemented by derived type and represents actual asynchronous operation to be run on background
        /// </summary>
        /// <param name="stoppingToken"><see cref="CancellationToken" /> for cancellation signaling</param>
        /// <returns>Awaitable task, representing actual background work</returns>
        protected abstract Task ExecuteAsync(CancellationToken stoppingToken);

        /// <summary>
        /// Starts synchronously and continues to run work asynchronously on the background
        /// </summary>
        public void Start()
        {
            if(disposed) throw new ObjectDisposedException(GetType().Name);

            worker = Interlocked.CompareExchange(ref stateSentinel, 1, 0) switch
            {
                0 => StartWorkerAsync(default),
                _ => throw new InvalidOperationException(AlreadyRunningMessage)
            };
        }

        /// <summary>
        /// Starts work and returns task that represent running asynchronous work
        /// </summary>
        /// <param name="stoppingToken"><see cref="CancellationToken" /> that signals about external cancellation</param>
        /// <returns>Awaitable task that represents currently running operation</returns>
        public Task RunAsync(CancellationToken stoppingToken)
        {
            if(disposed) throw new ObjectDisposedException(GetType().Name);

            return Interlocked.CompareExchange(ref stateSentinel, 1, 0) switch
            {
                0 => worker = StartWorkerAsync(stoppingToken),
                _ => throw new InvalidOperationException(AlreadyRunningMessage)
            };
        }

        /// <summary>
        /// Signals currently running asynchronous work about completion request
        /// </summary>
        /// <returns>Awaitable task which represents result of background work completion</returns>
        public async Task StopAsync()
        {
            var localWorker = worker;
            var localCts = globalCts;

            switch(Interlocked.CompareExchange(ref stateSentinel, 2, 1))
            {
                case 1:
                    using(localCts)
                    {
                        localCts.Cancel();
                        await localWorker.ConfigureAwait(false);
                    }

                    break;
                case 2:
                    await localWorker.ConfigureAwait(false);
                    break;
            }
        }

        private async Task StartWorkerAsync(CancellationToken stoppingToken)
        {
            var cts = new CancellationTokenSource();
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, stoppingToken);
            try
            {
                globalCts = cts;
                await ExecuteAsync(linkedCts.Token).ConfigureAwait(false);
            }
            catch(OperationCanceledException) {}
            finally
            {
                linkedCts.Dispose();
                if(Interlocked.Exchange(ref stateSentinel, 0) == 1) cts.Dispose();
            }
        }
    }
}