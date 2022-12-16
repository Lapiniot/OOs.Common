namespace System.IO.Pipelines;

/// <summary>
/// Provides base abstract class for pipe data consumer.
/// </summary>
public abstract class PipeConsumer : PipeConsumerCore
{
    private readonly PipeReader reader;
    private CancellationTokenSource abortTokenSource;
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

    protected CancellationToken Aborted => abortTokenSource.Token;

    protected override Task StartingAsync(CancellationToken cancellationToken)
    {
        abortTokenSource = new();
        consumerTask = StartConsumerAsync(reader, abortTokenSource.Token);
        return Task.CompletedTask;
    }

    protected override async Task StoppingAsync()
    {
        using (abortTokenSource)
        {
            // Try to perform graceful consumer loop cancellation first of all, 
            // by cancelling current pending reader.ReadAsync() call  
            reader.CancelPendingRead();

            try
            {
                await consumerTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { /* expected */ }
        }
    }

    /// <summary>
    /// Abruptly aborts current processing task 
    /// </summary>
    public void Abort() => abortTokenSource?.Cancel();

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        using (abortTokenSource)
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}