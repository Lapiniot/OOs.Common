namespace System.Threading;

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
        if (Interlocked.CompareExchange(ref disposed, 1, 0) != 0) return;

        using (localCts)
        using (jointCts)
        {
            try
            {
                localCts.Cancel();
                await completion.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
        }
    }

    #endregion
}