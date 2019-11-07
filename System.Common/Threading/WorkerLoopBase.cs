using System.Diagnostics;
using System.Threading.Tasks;
using static System.Threading.Tasks.TaskContinuationOptions;

namespace System.Threading
{
    public abstract class WorkerLoopBase<T> : IAsyncDisposable
    {
        private readonly T localState;
        private readonly Func<T, CancellationToken, Task> worker;
        private bool disposed;
        private Task processorTask;
        private CancellationTokenSource tokenSource;

        protected WorkerLoopBase(Func<T, CancellationToken, Task> worker, T state)
        {
            this.worker = worker ?? throw new ArgumentNullException(nameof(worker));
            localState = state;
        }

        public bool Running => Volatile.Read(ref tokenSource) != null;

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
                try
                {
                    await (processorTask = RunAsync(localState, tcs.Token)).ConfigureAwait(false);
                }
                catch(OperationCanceledException) {}
            }
        }

        public void Stop()
        {
            Interlocked.Exchange(ref tokenSource, null)?.Cancel();
        }

        public async ValueTask StopAsync()
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
                catch(OperationCanceledException) {}
            }
        }

        protected abstract Task RunAsync(T state, CancellationToken cancellationToken);

        protected void CheckDisposed()
        {
            if(disposed) throw new ObjectDisposedException(null);
        }
    }
}