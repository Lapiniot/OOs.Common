using static System.Threading.Tasks.Task;

namespace System.Threading;

public sealed class IntervalWorkerLoop : Worker
{
    private readonly Func<CancellationToken, Task> asyncWork;
    private readonly TimeSpan interval;

    public IntervalWorkerLoop(Func<CancellationToken, Task> asyncWork, TimeSpan interval)
    {
        ArgumentNullException.ThrowIfNull(asyncWork);

        this.asyncWork = asyncWork;
        this.interval = interval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await asyncWork(stoppingToken).ConfigureAwait(false);
            await Delay(interval, stoppingToken).ConfigureAwait(false);
        }
    }
}