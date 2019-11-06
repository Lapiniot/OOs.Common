using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Threading
{
    public class IntervalWorkerLoop<T> : WorkerLoopBase<T>
    {
        private readonly TimeSpan interval;

        public IntervalWorkerLoop(Func<T, CancellationToken, Task> asyncWork, T state, TimeSpan interval) :
            base(asyncWork, state)
        {
            this.interval = interval;
        }

        protected override async Task RunAsync(T state, CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await DoWorkAsync(state, cancellationToken).ConfigureAwait(false);
                }
                catch(OperationCanceledException) {}

                await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}