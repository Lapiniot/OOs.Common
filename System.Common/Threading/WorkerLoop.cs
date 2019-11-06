using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Threading
{
    public class WorkerLoop<T> : WorkerLoopBase<T>
    {
        public WorkerLoop(Func<T, CancellationToken, Task> asyncWork, T state) :
            base(asyncWork, state)
        { }


        protected override async Task RunAsync(T state, CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await DoWorkAsync(state, cancellationToken).ConfigureAwait(false);
                }
                catch(OperationCanceledException) { }
            }
        }
    }
}