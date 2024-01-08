namespace OOs.Threading;

public sealed class CancelableOperationScope : IAsyncCancelable
{
    private readonly Task completion;
    private readonly CancellationTokenSource jointCts;
    private readonly CancellationTokenSource localCts;
    private int disposed;

    private CancelableOperationScope(Func<CancellationToken, Task> operation, CancellationToken stoppingToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        localCts = new();

        var token = stoppingToken != default
            ? (jointCts = CancellationTokenSource.CreateLinkedTokenSource(localCts.Token, stoppingToken)).Token
            : localCts.Token;

        try
        {
            completion = operation(token);
        }
        catch
        {
            using (localCts)
            using (jointCts) { }

            throw;
        }
    }

    public static CancelableOperationScope Start(Func<CancellationToken, Task> operation, CancellationToken stoppingToken = default) =>
        new(operation, stoppingToken);

    #region Implementation of IAsyncCancelable

    bool IAsyncCancelable.IsCompleted => completion.IsCompleted;

    bool IAsyncCancelable.IsCanceled => completion.IsCanceled;

    Exception IAsyncCancelable.Exception => completion.Exception;

    public Task Completion => completion;

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;

        using (localCts)
        using (jointCts)
        {
            try
            {
                await localCts.CancelAsync().ConfigureAwait(false);
                await completion.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
        }
    }

    #endregion
}