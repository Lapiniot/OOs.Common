namespace System.Threading;

public sealed class WorkerLoop : Worker
{
    private readonly Func<CancellationToken, Task> asyncWork;

    public WorkerLoop(Func<CancellationToken, Task> asyncWork)
    {
        ArgumentNullException.ThrowIfNull(asyncWork);
        this.asyncWork = asyncWork;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            await asyncWork(stoppingToken).ConfigureAwait(false);
        }
    }
}