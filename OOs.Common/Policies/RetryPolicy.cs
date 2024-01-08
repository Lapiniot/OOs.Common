using static System.Threading.Tasks.Task;

namespace OOs.Policies;

public abstract class RetryPolicy : IRetryPolicy
{
    protected abstract bool ShouldRetry(Exception exception, int attempt, TimeSpan totalTime, ref TimeSpan delay);

    #region Implementation of IRetryPolicy

    public async Task<T> RetryAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var attempt = 1;
        var delay = TimeSpan.Zero;
        var startedAt = DateTime.UtcNow;

        while (true)
        {
            try
            {
                return await operation(cancellationToken) // This is a protection step for the operations that do not handle cancellation properly by themselves.
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException oce) when (oce.CancellationToken == cancellationToken)
            {
                throw;
            }
            catch (Exception exception)
            {
                if (!ShouldRetry(exception, attempt, DateTime.UtcNow - startedAt, ref delay))
                    throw;
            }

            await Delay(delay, cancellationToken).ConfigureAwait(false);

            attempt++;
        }
    }

    #endregion
}