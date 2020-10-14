using System.Threading;
using System.Threading.Tasks;
using static System.Threading.Tasks.Task;

namespace System.Policies
{
    public abstract class RepeatPolicy : IRepeatPolicy
    {
        protected abstract bool ShouldRepeat(Exception exception, int attempt, TimeSpan totalTime, ref TimeSpan delay);

        #region Implementation of IRetryPolicy

        public async Task RepeatAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
        {
            if(operation is null) throw new ArgumentNullException(nameof(operation));

            var attempt = 1;
            var delay = TimeSpan.Zero;
            var startedAt = DateTime.UtcNow;

            while(!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    try
                    {
                        await operation(cancellationToken)
                            // This is a protection step for the operations that do not handle cancellation properly by themselves.
                            // In case of external cancellation, WaitAsync transits to Cancelled state, throwing OperationCancelled exception,
                            // and terminates retry loop. Original async operation may still be in progress, but we give up in order
                            // to stop retry loop as soon as possible
                            .WaitAsync(cancellationToken)
                            .ConfigureAwait(false);

                        if(!ShouldRepeat(null, attempt, DateTime.UtcNow - startedAt, ref delay))
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
                        if(!ShouldRepeat(e, attempt, DateTime.UtcNow - startedAt, ref delay))
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