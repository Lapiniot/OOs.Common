using System.Threading;
using System.Threading.Tasks;
using static System.Threading.Tasks.Task;

namespace System.Policies
{
    public abstract class RetryPolicy : IRetryPolicy
    {
        protected abstract bool ShouldRetry(Exception exception, int attempt, TimeSpan totalTime, ref TimeSpan delay);

        #region Implementation of IRetryPolicy

        public async Task RetryAsync(Func<CancellationToken, Task> worker, CancellationToken cancellationToken)
        {
            if(worker is null) throw new ArgumentNullException(nameof(worker));

            var attempt = 1;
            var delay = TimeSpan.Zero;
            var startedAt = DateTime.UtcNow;

            while(!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    try
                    {
                        await worker(cancellationToken)
                            // This is a protection step for the operations that do not handle cancellation properly by themselves.
                            // In case of external cancellation, WaitAsync transits to Cancelled state, throwing OperationCancelled exception,
                            // and terminates retry loop. Original async operation may still be in progress, but we give up in order
                            // to stop retry loop as soon as possible
                            .WaitAsync(cancellationToken)
                            .ConfigureAwait(false);

                        if(!ShouldRetry(null, attempt, DateTime.UtcNow - startedAt, ref delay))
                        {
                            break;
                        }
                    }
                    catch(OperationCanceledException)
                    {
                        break;
                    }
                    catch(Exception e)
                    {
                        if(!ShouldRetry(e, attempt, DateTime.UtcNow - startedAt, ref delay))
                        {
                            throw;
                        }
                    }

                    await Delay(delay, cancellationToken).ConfigureAwait(false);

                    attempt++;
                }
                catch(OperationCanceledException)
                {
                }
            }
        }

        #endregion
    }
}