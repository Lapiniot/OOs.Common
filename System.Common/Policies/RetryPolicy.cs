using System.Threading;
using System.Threading.Tasks;

namespace System.Policies
{
    public abstract class RetryPolicy : IRetryPolicy
    {
        protected abstract bool ShouldRetry(int attempt, TimeSpan totalTime, ref TimeSpan delay);

        #region Implementation of IRetryPolicy

        public Task RetryAsync(Func<CancellationToken, Task> asyncFunc, CancellationToken cancellationToken)
        {
            return RetryAsync(async c =>
            {
                await asyncFunc(c).ConfigureAwait(false);
                return true;
            }, cancellationToken);
        }

        public async Task<T> RetryAsync<T>(Func<CancellationToken, Task<T>> asyncFunc, CancellationToken cancellationToken)
        {
            var attempt = 1;
            var delay = TimeSpan.Zero;
            var startedAt = DateTime.UtcNow;

            while(true)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return await asyncFunc(cancellationToken).ConfigureAwait(false);
                }
                catch(OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    if(!ShouldRetry(attempt, DateTime.UtcNow - startedAt, ref delay))
                    {
                        throw;
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);

                attempt++;
            }
        }

        #endregion
    }
}