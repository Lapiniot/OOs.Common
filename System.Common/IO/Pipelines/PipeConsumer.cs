namespace System.IO.Pipelines;

/// <summary>
/// Provides base abstract class for pipe data consumer.
/// </summary>
public abstract class PipeConsumer : PipeConsumerCore
{
    private readonly PipeReader reader;
    private CancellationTokenSource cancellationTokenSource;
    private Task consumerTask;

    protected PipeConsumer(PipeReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        this.reader = reader;
    }

    protected Task ConsumerCompletion
    {
        get
        {
            Verify.ThrowIfInvalidState(!IsRunning);
            return consumerTask;
        }
    }

    protected override Task StartingAsync(CancellationToken cancellationToken)
    {
        cancellationTokenSource = new();
        consumerTask = StartConsumerAsync(reader, cancellationTokenSource.Token);
        return Task.CompletedTask;
    }

    protected override async Task StoppingAsync()
    {
        using (cancellationTokenSource)
        {
            // Try to perform graceful consumer loop cancellation first of all, 
            // by cancelling current pending reader.ReadAsync() call  
            reader.CancelPendingRead();

            try
            {
                await consumerTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // expected
            }
        }
    }

    /// <summary>
    /// Abruptly aborts current consumer task making it return with <see cref="OperationCanceledException" />
    /// </summary>
    public virtual void Abort() => cancellationTokenSource?.Cancel();
}