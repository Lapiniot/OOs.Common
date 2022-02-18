using System.Buffers;

namespace System.IO.Pipelines;

/// <summary>
/// Provides base pipe processor implementation which runs two loops.
/// Producer loop reads data from the abstract source asynchronously on
/// arrival via <see cref="ReceiveAsync" /> and writes to the pipe.
/// Consumer loop consumes data from the pipe via <see cref="Consume" />.
/// </summary>
public abstract class PipeProducerConsumer : ActivityObject
{
    private CancellationTokenSource cancellationTokenSource;
    private Task processor;

    protected override Task StartingAsync(CancellationToken cancellationToken)
    {
        cancellationTokenSource = new();
        var token = cancellationTokenSource.Token;

        var (reader, writer) = new Pipe();

        var producer = StartProducerAsync(writer, token);
        var consumer = StartConsumerAsync(reader, token);
        processor = Task.WhenAll(producer, consumer);

        return Task.CompletedTask;
    }

    protected override async Task StoppingAsync()
    {
        using (cancellationTokenSource)
        {
            cancellationTokenSource.Cancel();
            await processor.ConfigureAwait(false);
        }
    }

    private async Task StartProducerAsync(PipeWriter writer, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
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

    private async Task StartConsumerAsync(PipeReader reader, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);

                var buffer = result.Buffer;

                if (buffer.Length > 0)
                {
                    Consume(in buffer, out var consumed);

                    if (consumed > 0)
                    {
                        reader.AdvanceTo(buffer.GetPosition(consumed));
                        continue;
                    }

                    // Seems we have not enough data to be consumed in a consistent way by the consumer logic
                    reader.AdvanceTo(buffer.Start, buffer.End);
                }

                if (result.IsCompleted || result.IsCanceled)
                {
                    // However we couldn't get more data, because writer end has already completed writing. 
                    // So we better terminate reading in order to avoid potential "dead" loop
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
            await reader.CompleteAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Provides abstract data reading support from asynchronous sources (network stream, socket e.g.)
    /// </summary>
    /// <param name="buffer">Memory buffer to be filled with data</param>
    /// <param name="cancellationToken"><see cref="CancellationToken" /> for external cancellation support.</param>
    /// <returns>Actual amount of the data received.</returns>
    protected abstract ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);

    /// <summary>
    /// Method gets called every time new data is available.
    /// </summary>
    /// <param name="sequence">Sequence of linked buffers containing data produced by the pipe writer</param>
    /// <param name="consumed">Amount of bytes actually consumed by our implementation or <value>0</value> if no data can be consumed at the moment.</param>
    protected abstract void Consume(in ReadOnlySequence<byte> sequence, out long consumed);
}