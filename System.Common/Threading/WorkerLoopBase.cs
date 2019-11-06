using System.Diagnostics;
using System.Threading.Tasks;
using static System.Threading.Tasks.TaskContinuationOptions;

namespace System.Threading
{
    public abstract class WorkerLoopBase<T> : IDisposable
    {
        private readonly T state;
        private readonly Func<T, CancellationToken, Task> worker;
        private bool disposed;
        private Task processorTask;
        private CancellationTokenSource tokenSource;

        protected WorkerLoopBase(Func<T, CancellationToken, Task> worker, T state)
        {
            this.worker = worker ?? throw new ArgumentNullException(nameof(worker));
            this.state = state;
        }

        public bool Running => Volatile.Read(ref tokenSource) != null;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected Task DoWorkAsync(T state, CancellationToken cancellation)
        {
            return worker(state, cancellation);
        }

        public void Start()
        {
            CheckDisposed();

            var _ = StartAsync().ContinueWith(t => Trace.TraceError(t.Exception?.GetBaseException().ToString()), default, NotOnRanToCompletion, TaskScheduler.Default);
        }

        private async Task StartAsync()
        {
            using var tcs = new CancellationTokenSource();
            if(Interlocked.CompareExchange(ref tokenSource, tcs, null) == null)
            {
                processorTask = RunAsync(state, tcs.Token);
                await processorTask.ConfigureAwait(false);
            }
        }

        public void Stop()
        {
            Interlocked.Exchange(ref tokenSource, null)?.Cancel();
        }

        public async Task StopAsync()
        {
            var task = processorTask;

            var tcs = Interlocked.Exchange(ref tokenSource, null);

            if(tcs != null)
            {
                tcs.Cancel();

                try
                {
                    await task.ConfigureAwait(false);
                }
                catch(OperationCanceledException)
                {
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