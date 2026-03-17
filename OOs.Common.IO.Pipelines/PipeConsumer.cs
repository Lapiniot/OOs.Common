using System.IO.Pipelines;

#nullable enable

namespace OOs.IO.Pipelines;

/// <summary>
/// Provides base abstract class for pipe data consumer.
/// </summary>
public abstract class PipeConsumer : PipeConsumerCore
{
    private readonly PipeReader reader;
    private CancellationTokenSource abortTokenSource;

    protected PipeConsumer(PipeReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        this.reader = reader;
        abortTokenSource = new();
    }

    protected Task? ConsumerCompletion { get; private set; }

    protected CancellationToken Aborted => abortTokenSource.Token;

    protected override Task StartingAsync(CancellationToken cancellationToken)
    {
        ResetAbortTokenSource();
        ConsumerCompletion = RunConsumerAsync(reader, abortTokenSource.Token);
        return Task.CompletedTask;
    }

    protected override async Task StoppingAsync()
    {
        // Try to perform graceful consumer loop cancellation first of all, 
        // by cancelling current pending reader.ReadAsync() call  
        reader.CancelPendingRead();

        try
        {
            await ConsumerCompletion!.ConfigureAwait(false);
        }
        catch (OperationCanceledException) { /* expected */ }
    }

    private void ResetAbortTokenSource()
    {
        if (!abortTokenSource.TryReset())
        {
            using (abortTokenSource)
            {
                abortTokenSource = new();
            }
        }
    }

    protected void Abort() => abortTokenSource.Cancel();

    protected void AbortAsync() => abortTokenSource.CancelAsync();

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        using (abortTokenSource)
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}