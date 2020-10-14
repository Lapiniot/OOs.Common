using System.Threading;
using System.Threading.Tasks;
using static System.Threading.Tasks.Task;

namespace System.Policies
{
    public abstract class RetryPolicy : IRetryPolicy
    {
        protected abstract bool ShouldRetry(Exception exception, int attempt, TimeSpan totalTime, ref TimeSpan delay);

        #region Implementation of IRetryPolicy

        public async Task<T> RetryAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
        {
            if(operation is null) throw new ArgumentNullException(nameof(operation));

            var attempt = 1;
            var delay = TimeSpan.Zero;
            var startedAt = DateTime.UtcNow;

            while(true)
            {
                try
                {
                    return await operation(cancellationToken)
                        // This is a protection step for the operations that do not handle cancellation properly by themselves.
                        // In case of external cancellation, WaitAsync transits to Cancelled state, throwing OperationCancelled exception,
                        // and terminates retry loop. Original async operation may still be in progress, but we give up in order
                        // to stop retry loop as soon as possible
                        .WaitAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                catch(OperationCanceledException)
                {
                    throw;
                }
                catch(Exception exception)
                {
                    if(!ShouldRetry(exception, attempt, DateTime.UtcNow - startedAt, ref delay))
                    {
                        throw;
                    }
                }

                await Delay(delay, cancellationToken).ConfigureAwait(false);

                attempt++;
            }
        }

        #endregion
    }
}