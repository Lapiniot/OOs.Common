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
            Volatile.Read(ref resetSource)?.Cancel();
        }

        #region Overrides of WorkerBase

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ResetCancellationState(out var tokenSource, out var linkedSource);

            try
            {
                while(!stoppingToken.IsCancellationRequested &&
                      (maxIterations == Infinite || iteration < maxIterations))
                {
                    try
                    {
                        await Task.Delay(delay, linkedSource.Token).ConfigureAwait(false);
                        await asyncWork(stoppingToken).ConfigureAwait(false);
                        iteration++;
                    }
                    catch(OperationCanceledException)
                    {
                        if(stoppingToken.IsCancellationRequested) break;
                        linkedSource.Dispose();
                        ResetCancellationState(out tokenSource, out linkedSource).Dispose();
                    }
                }
            }
            finally
            {
                Interlocked.Exchange(ref resetSource, null);
                linkedSource.Dispose();
                tokenSource.Dispose();
            }

            CancellationTokenSource ResetCancellationState(out CancellationTokenSource resetTokenSource, out CancellationTokenSource linkedTokenSource)
            {
                resetTokenSource = new CancellationTokenSource();
                linkedTokenSource = CreateLinkedTokenSource(stoppingToken, resetTokenSource.Token);
                return Interlocked.Exchange(ref resetSource, resetTokenSource);
            }
        }

        #endregion
    }
}