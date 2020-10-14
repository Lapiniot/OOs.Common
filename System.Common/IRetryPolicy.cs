using System.Threading;
using System.Threading.Tasks;

namespace System
{
    public interface IRetryPolicy
    {
        Task RetryAsync(Func<CancellationToken, Task> worker, CancellationToken cancellationToken = default);
    }
}