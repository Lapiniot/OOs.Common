using System.Threading.Tasks;
using static System.Threading.CancellationTokenSource;

namespace System.Threading
{
    public class DelayWorkerLoop : WorkerLoopBase
    {
        private readonly TimeSpan delay;
        private readonly int maxIterations;
        private int iteration;
        private CancellationTokenSource resetSource;

        public DelayWorkerLoop(Func<CancellationToken, Task> asyncWork,
            TimeSpan delay, int maxIterations = -1) : base(asyncWork)
        {
            this.delay = delay;
            this.maxIterations = maxIterations;
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            ResetDelay();

            while(!cancellationToken.IsCancellationRequested &&
                  (maxIterations == -1 || iteration < maxIterations))
            {
                using(var linkedSource = CreateLinkedTokenSource(cancellationToken, resetSource.Token))
                {
                    try
                    {
                        await Task.Delay(delay, linkedSource.Token).ConfigureAwait(false);
                        await AsyncWork(cancellationToken).ConfigureAwait(false);
                        iteration++;
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }

        public void ResetDelay()
        {
            using(var source = Interlocked.Exchange(ref resetSource, new CancellationTokenSource()))
            {
                source?.Cancel();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            resetSource.Dispose();
        }
    }
}