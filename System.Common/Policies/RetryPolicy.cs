﻿using System.Threading;
using System.Threading.Tasks;
using static System.Threading.Tasks.Task;

namespace System.Policies
{
    public abstract class RetryPolicy : IRetryPolicy
    {
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMilliseconds(-1);

        protected abstract bool ShouldRetry(Exception exception, int attempt, TimeSpan totalTime, ref TimeSpan delay);

        #region Implementation of IRetryPolicy

        public Task RetryAsync(Func<CancellationToken, Task> worker, CancellationToken cancellationToken)
        {
            return RetryAsync(async c =>
            {
                await worker(c).ConfigureAwait(false);
                return true;
            }, cancellationToken);
        }

        public async Task<T> RetryAsync<T>(Func<CancellationToken, Task<T>> worker, CancellationToken cancellationToken)
        {
            if(worker is null) throw new ArgumentNullException(nameof(worker));

            var attempt = 1;
            var delay = TimeSpan.Zero;
            var startedAt = DateTime.UtcNow;

            using var timeoutTokenSource = new CancellationTokenSource(Timeout);
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutTokenSource.Token, cancellationToken);

            var token = linkedTokenSource.Token;
            while(true)
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    return await worker(token)
                        // This is a protection step for the operations that do not handle cancellation properly by themselves.
                        // In case of external cancellation, WaitAsync transits to Cancelled state, throwing OperationCancelled exception,
                        // and terminates retry loop. Original async operation may still be in progress, but we give up in order
                        // to stop retry loop as soon as possible
                        .WaitAsync(token)
                        .ConfigureAwait(false);
                }
                catch(OperationCanceledException)
                {
                    throw;
                }
                catch(Exception e)
                {
                    if(!ShouldRetry(e, attempt, DateTime.UtcNow - startedAt, ref delay))
                    {
                        throw;
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                await Delay(delay, cancellationToken).ConfigureAwait(false);

                attempt++;
            }
        }

        #endregion
    }
}