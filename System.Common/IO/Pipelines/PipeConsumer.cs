using System.Buffers;
using System.Properties;

namespace System.IO.Pipelines;

/// <summary>
/// Provides base abstract class for pipe data consumer.
/// </summary>
public abstract class PipeConsumer : ActivityObject
{
    private readonly PipeReader reader;
    private CancellationTokenSource cancellationTokenSource;
    private Task consumer;

    protected PipeConsumer(PipeReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        this.reader = reader;
    }

    public Task Completion => IsRunning ? consumer : throw new InvalidOperationException(Strings.PipeNotStarted);

    protected override Task StartingAsync(CancellationToken cancellationToken)
    {
        cancellationTokenSource = new CancellationTokenSource();

        consumer = StartConsumerAsync(cancellationTokenSource.Token);

        return Task.CompletedTask;
    }

    protected override async Task StoppingAsync()
    {
        using(cancellationTokenSource)
        {
            cancellationTokenSource.Cancel();

            await consumer.ConfigureAwait(false);
        }
    }

    private async Task StartConsumerAsync(CancellationToken token)
    {
        try
        {
            while(!token.IsCancellationRequested)
            {
                var rt = reader.ReadAsync(token);
                var result = rt.IsCompletedSuccessfully ? rt.Result : await rt.ConfigureAwait(false);

                var buffer = result.Buffer;

                if(buffer.Length > 0)
                {
                    Consume(buffer, out var consumed);

                    if(consumed > 0)
                    {
                        reader.AdvanceTo(buffer.GetPosition(consumed));
                        continue;
                    }
                    else
                    {
                        // Seems we have not enough data to be consumed in a consistent way by the consumer logic
                        reader.AdvanceTo(buffer.Start, buffer.End);
                    }
                }

                if(result.IsCompleted || result.IsCanceled)
                {
                    // However we couldn't get more data, because writer end has already completed writing. 
                    // So we better terminate reading in order to avoid potential "dead" loop
                    break;
                }
            }

            OnCompleted();
        }
        catch(OperationCanceledException)
        {
            OnCompleted();
        }
        catch(Exception exception) when(OnCompleted(exception))
        {
            // Suppress exception if OnCompleted returns true (handled as expected)
        }
        finally
        {
            await reader.CompleteAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Method gets called every time new data is available.
    /// </summary>
    /// <param name="sequence">Sequence of linked buffers containing data produced by the pipe writer</param>
    /// <param name="bytesConsumed">Amount of bytes actually consumed by our implementation or <value>0</value> if no data can be consumed at the moment.</param>
    protected abstract void Consume(in ReadOnlySequence<byte> sequence, out long bytesConsumed);

    /// <summary>
    /// Method gets called when consumer completed its work
    /// </summary>
    /// <param name="exception">Should exceptions occur, last value is passed</param>
    /// <returns><value>True</value> if exception is handled and shouldn't be rethrown</returns>
    protected abstract bool OnCompleted(Exception exception = null);
}