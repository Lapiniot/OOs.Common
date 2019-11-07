using System.Threading;
using System.Threading.Tasks;

namespace System
{
    public interface IRetryPolicy
    {
        Task RetryAsync(Func<CancellationToken, Task> worker, CancellationToken cancellationToken = default);
        Task<T> RetryAsync<T>(Func<CancellationToken, Task<T>> worker, CancellationToken cancellationToken = default);
    }
}