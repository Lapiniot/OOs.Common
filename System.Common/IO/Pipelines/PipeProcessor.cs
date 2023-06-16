namespace System.IO.Pipelines;

/// <summary>
/// Provides base pipe processor implementation which runs two loops.
/// Producer loop reads data from the abstract source asynchronously on
/// arrival via <see cref="ReceiveAsync" /> and writes to the pipe.
/// Consumer loop consumes data from the pipe via <see cref="PipeConsumerCore.Consume" />.
/// </summary>
public abstract class PipeProcessor : PipeConsumerCore
{
    private CancellationTokenSource abortTokenSource;
    private Task processor;

    protected override Task StartingAsync(CancellationToken cancellationToken)
    {
        abortTokenSource = new();
        var token = abortTokenSource.Token;

        var (reader, writer) = new Pipe();

        var producer = StartProducerAsync(writer, token);
        var consumer = RunConsumerAsync(reader, token);
        processor = Task.WhenAll(producer, consumer);

        return Task.CompletedTask;
    }

    protected override async Task StoppingAsync()
    {
        using (abortTokenSource)
        {
            abortTokenSource.Cancel();
            await processor.ConfigureAwait(false);
        }
    }

    private async Task StartProducerAsync(PipeWriter writer, CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var buffer = writer.GetMemory();

                var received = await ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);

                if (received == 0)
                {
                    break;
                }

                writer.Advance(received);

                var result = await writer.FlushAsync(cancellationToken).ConfigureAwait(false);

                if (result.IsCompleted || result.IsCanceled)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        finally
        {
            await writer.CompleteAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Provides abstract data reading support from asynchronous sources (network stream, socket e.g.)
    /// </summary>
    /// <param name="buffer">Memory buffer to be filled with data</param>
    /// <param name="cancellationToken"><see cref="CancellationToken" /> for external cancellation support.</param>
    /// <returns>Actual amount of the data received.</returns>
    protected abstract ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        using (abortTokenSource)
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}