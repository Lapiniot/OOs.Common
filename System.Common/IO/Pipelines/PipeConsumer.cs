namespace System.IO.Pipelines;

/// <summary>
/// Provides base abstract class for pipe data consumer.
/// </summary>
public abstract class PipeConsumer : PipeConsumerCore
{
    private readonly PipeReader reader;
    private CancellationTokenSource abortTokenSource = new();

    protected PipeConsumer(PipeReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        this.reader = reader;
    }

    protected Task ConsumerCompletion { get; private set; }

    protected CancellationToken Aborted => abortTokenSource.Token;

    protected override Task StartingAsync(CancellationToken cancellationToken)
    {
        abortTokenSource ??= new();
        ConsumerCompletion = RunConsumerAsync(reader, Aborted);
        return Task.CompletedTask;
    }

    protected override async Task StoppingAsync()
    {
        using (abortTokenSource)
        {
            abortTokenSource = null;
            // Try to perform graceful consumer loop cancellation first of all, 
            // by cancelling current pending reader.ReadAsync() call  
            reader.CancelPendingRead();

            try
            {
                await ConsumerCompletion.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { /* expected */ }
        }
    }

    protected void Abort() => abortTokenSource?.Cancel();

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        using (abortTokenSource)
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}