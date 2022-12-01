using System.Buffers;

namespace System.IO.Pipelines;

public abstract class PipeConsumerCore : ActivityObject
{
    protected async Task StartConsumerAsync(PipeReader reader, CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);

                var buffer = result.Buffer;

                while (Consume(ref buffer))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                reader.AdvanceTo(buffer.Start, buffer.End);

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
            await reader.CompleteAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Method gets called every time new data is available.
    /// </summary>
    /// <param name="buffer">Incoming data buffer.</param>
    /// <returns><see langword="true" /> if data was successfully consumed.</returns>
    /// <remarks>Implementations should update <paramref name="buffer" /> reference
    /// with unprocessed buffer reminder.</remarks>
    protected abstract bool Consume(ref ReadOnlySequence<byte> buffer);
}