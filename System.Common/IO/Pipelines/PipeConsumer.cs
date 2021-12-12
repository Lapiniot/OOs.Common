﻿using System.Buffers;
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

    public Task Completion => IsRunning ? consumer : throw new InvalidOperationException(Strings.InvalidStateNotStarted);

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
                var vt = reader.ReadAsync(token);
                var result = vt.IsCompletedSuccessfully ? vt.Result : await vt.ConfigureAwait(false);

                var buffer = result.Buffer;

                if(buffer.Length > 0)
                {
                    Consume(in buffer, out var consumed);

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
        }
        catch(OperationCanceledException)
        {
            // Expected
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
    /// <param name="consumed">Amount of bytes actually consumed by our implementation or <value>0</value> if no data can be consumed at the moment.</param>
    protected abstract void Consume(in ReadOnlySequence<byte> sequence, out long consumed);
}