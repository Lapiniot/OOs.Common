using System.Threading.Tasks;
using static System.Threading.CancellationTokenSource;

namespace System.Threading
{
    public sealed class DelayWorkerLoop : WorkerBase
    {
        public const int Infinite = -1;
        private readonly Func<CancellationToken, Task> asyncWork;
        private readonly TimeSpan delay;
        private readonly int maxIterations;
        private int iteration;
        private CancellationTokenSource resetSource;

        public DelayWorkerLoop(Func<CancellationToken, Task> asyncWork, TimeSpan delay, int maxIterations = Infinite)
        {
            this.asyncWork = asyncWork ?? throw new ArgumentNullException(nameof(asyncWork));
            this.delay = delay;
            this.maxIterations = maxIterations;
        }

        public void ResetDelay()
        {
            using var source = Interlocked.Exchange(ref resetSource, new CancellationTokenSource());
            source?.Cancel();
        }

        #region Overrides of WorkerBase

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            ResetDelay();

            while(!cancellationToken.IsCancellationRequested &&
                  (maxIterations == Infinite || iteration < maxIterations))
            {
                using var linkedSource = CreateLinkedTokenSource(cancellationToken, resetSource.Token);

                try
                {
                    await Task.Delay(delay, linkedSource.Token).ConfigureAwait(false);
                    await asyncWork(cancellationToken).ConfigureAwait(false);
                    iteration++;
                }
                catch(OperationCanceledException)
                {
                }
            }
        }

        #endregion
    }
}