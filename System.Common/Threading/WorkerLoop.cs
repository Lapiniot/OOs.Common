using System.Threading.Tasks;

namespace System.Threading
{
    public class WorkerLoop : WorkerLoopBase
    {
        public WorkerLoop(Func<CancellationToken, Task> asyncWork) : base(asyncWork)
        {
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
            }
        }
    }
}