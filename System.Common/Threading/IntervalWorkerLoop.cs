using System.Threading.Tasks;
using static System.Threading.Tasks.Task;

namespace System.Threading
{
    public sealed class IntervalWorkerLoop : WorkerBase
    {
        private readonly Func<CancellationToken, Task> asyncWork;
        private readonly TimeSpan interval;

        public IntervalWorkerLoop(Func<CancellationToken, Task> asyncWork, TimeSpan interval)
        {
            this.asyncWork = asyncWork ?? throw new ArgumentNullException(nameof(asyncWork));
            this.interval = interval;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                await asyncWork(stoppingToken).ConfigureAwait(false);
                await Delay(interval, stoppingToken).ConfigureAwait(false);
            }
        }
    }
}