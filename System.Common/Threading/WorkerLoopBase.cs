using System.Threading.Tasks;

namespace System.Threading
{
    public abstract class WorkerLoopBase<T> : IDisposable
    {
        private readonly T state;
        protected Func<T, CancellationToken, Task> AsyncWork;
        private bool disposed;
        private Task processorTask;
        private CancellationTokenSource tokenSource;

        protected WorkerLoopBase(Func<T, CancellationToken, Task> asyncWork, T state)
        {
            AsyncWork = asyncWork ?? throw new ArgumentNullException(nameof(asyncWork));
            this.state = state;
        }

        public bool Running => Volatile.Read(ref tokenSource) != null;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Start()
        {
            CheckDisposed();

            var tcs = new CancellationTokenSource();

            if(Interlocked.CompareExchange(ref tokenSource, tcs, null) == null)
            {
                processorTask = RunAsync(state, tcs.Token);
            }
            else
            {
                tcs.Dispose();
            }
        }

        public void Stop()
        {
            var tcs = Interlocked.Exchange(ref tokenSource, null);

            if(tcs != null)
            {
                tcs.Cancel();
                tcs.Dispose();
            }
        }

        public async Task StopAsync()
        {
            var task = processorTask;

            var tcs = Interlocked.Exchange(ref tokenSource, null);

            if(tcs != null)
            {
                tcs.Cancel();
                tcs.Dispose();

                try
                {
                    await task.ConfigureAwait(false);
                }
                catch(OperationCanceledException)
                {
                    // ignored
                }
            }
        }

        protected abstract Task RunAsync(T state, CancellationToken cancellationToken);

        protected virtual void Dispose(bool disposing)
        {
            if(disposed) return;

            if(disposing)
            {
                Stop();
                processorTask.Dispose();
                disposed = true;
            }
        }

        protected void CheckDisposed()
        {
            if(disposed) throw new ObjectDisposedException(null);
        }
    }
}