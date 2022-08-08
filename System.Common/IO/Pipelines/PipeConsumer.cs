namespace System.IO.Pipelines;

/// <summary>
/// Provides base abstract class for pipe data consumer.
/// </summary>
public abstract class PipeConsumer : PipeConsumerCore
{
    private readonly PipeReader reader;
    private CancellationTokenSource cancellationTokenSource;
    private Task consumer;

    protected PipeConsumer(PipeReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        this.reader = reader;
    }

    public Task Completion
    {
        get
        {
            Verify.ThrowIfInvalidState(!IsRunning);
            return consumer;
        }
    }

    protected override Task StartingAsync(CancellationToken cancellationToken)
    {
        cancellationTokenSource = new();

        consumer = StartConsumerAsync(reader, cancellationTokenSource.Token);

        return Task.CompletedTask;
    }

    protected override async Task StoppingAsync()
    {
        using (cancellationTokenSource)
        {
            cancellationTokenSource.Cancel();

            await consumer.ConfigureAwait(false);
        }
    }
}