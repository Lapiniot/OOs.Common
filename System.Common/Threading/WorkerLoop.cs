using System.Threading.Tasks;

namespace System.Threading
{
    public sealed class WorkerLoop : WorkerBase
    {
        private readonly Func<CancellationToken, Task> asyncWork;

        public WorkerLoop(Func<CancellationToken, Task> asyncWork)
        {
            this.asyncWork = asyncWork ?? throw new ArgumentNullException(nameof(asyncWork));
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                await asyncWork(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}