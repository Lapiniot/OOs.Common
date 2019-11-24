using System.Threading.Tasks;

namespace System.Threading
{
    public abstract class WorkerBase : IAsyncDisposable
    {
        private int stateSentinel;
        private Task worker;
        private CancellationTokenSource globalCts;
        private bool disposed;

        public bool IsRunning => Volatile.Read(ref stateSentinel) == 1;

        public void Start()
        {
            if(disposed) throw new ObjectDisposedException(GetType().Name);

            worker = (Interlocked.CompareExchange(ref stateSentinel, 1, 0)) switch
            {
                0 => StartWorkerAsync(default),
                _ => throw new InvalidOperationException(Strings.AlreadyRunningMessage),
            };
        }

        public Task RunAsync(CancellationToken stoppingToken)
        {
            if(disposed) throw new ObjectDisposedException(GetType().Name);

            return Interlocked.CompareExchange(ref stateSentinel, 1, 0) switch
            {
                0 => worker = StartWorkerAsync(stoppingToken),
                _ => throw new InvalidOperationException(Strings.AlreadyRunningMessage)
            };
        }

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
            catch(OperationCanceledException) { }
            finally
            {
                linkedCts.Dispose();
                if(Interlocked.Exchange(ref stateSentinel, 0) == 1) cts.Dispose();
            }
        }

        protected abstract Task ExecuteAsync(CancellationToken cancellationToken);

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
    }
}