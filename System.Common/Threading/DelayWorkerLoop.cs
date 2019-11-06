using System.Diagnostics;
using System.Threading.Tasks;
using static System.Threading.CancellationTokenSource;

namespace System.Threading
{
    public class DelayWorkerLoop<T> : WorkerLoopBase<T>
    {
        private readonly TimeSpan delay;
        private readonly int maxIterations;
        private int iteration;
        private CancellationTokenSource resetSource;

        public DelayWorkerLoop(Func<T, CancellationToken, Task> asyncWork, T state,
            TimeSpan delay, int maxIterations = -1) : base(asyncWork, state)
        {
            this.delay = delay;
            this.maxIterations = maxIterations;
        }


        protected override async Task RunAsync(T state, CancellationToken cancellationToken)
        {
            ResetDelay();

            while(!cancellationToken.IsCancellationRequested &&
                  (maxIterations == -1 || iteration < maxIterations))
            {
                using var linkedSource = CreateLinkedTokenSource(cancellationToken, resetSource.Token);

                try
                {
                    await Task.Delay(delay, linkedSource.Token).ConfigureAwait(false);
                    await DoWorkAsync(state, cancellationToken).ConfigureAwait(false);
                    iteration++;
                }
                catch(OperationCanceledException) { }
            }
        }

        public void ResetDelay()
        {
            using var source = Interlocked.Exchange(ref resetSource, new CancellationTokenSource());
            source?.Cancel();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            resetSource.Dispose();
        }
    }
}