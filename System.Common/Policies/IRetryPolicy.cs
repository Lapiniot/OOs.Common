namespace System.Policies;

public interface IRetryPolicy
{
    Task<T> RetryAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
}