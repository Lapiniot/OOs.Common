using System.Threading;
using System.Threading.Tasks;

namespace System.Policies
{
    public class OneAttemptPolicy : IRetryPolicy
    {
        #region Implementation of IRetryPolicy

        public Task RetryAsync(Func<CancellationToken, Task> asyncFunc, CancellationToken cancellationToken)
        {
            return asyncFunc(cancellationToken);
        }

        public Task<T> RetryAsync<T>(Func<CancellationToken, Task<T>> asyncFunc, CancellationToken cancellationToken)
        {
            return asyncFunc(cancellationToken);
        }

        #endregion
    }
}