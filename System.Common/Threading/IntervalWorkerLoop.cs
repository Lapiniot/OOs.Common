using System.Threading.Tasks;

namespace System.Threading
{
    public class IntervalWorkerLoop : WorkerLoopBase
    {
        private readonly TimeSpan interval;

        public IntervalWorkerLoop(Func<CancellationToken, Task> asyncWork, TimeSpan interval) :
            base(asyncWork)
        {
            this.interval = interval;
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await AsyncWork(cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }

                await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}