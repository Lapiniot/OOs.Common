using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;

namespace OOs.IO.Pipelines;

public abstract class PipeConsumerCore : ActivityObject
{
    protected async Task RunConsumerAsync([NotNull] PipeReader reader, CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                if (result.IsCanceled)
                {
                    break;
                }

                var buffer = result.Buffer;
                Consume(ref buffer);
                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }
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
    /// <remarks>
    /// Implementations should update <paramref name="buffer" /> value before returning from the call 
    /// to indicate unprocessed buffer reminder.
    /// Please, notice: 
    /// <see cref="ReadOnlySequence{T}.Start" /> of the ref <paramref name="buffer"/> should contain 
    /// the extent of the data that has been successfully processed. Whereas
    /// <see cref="ReadOnlySequence{T}.End" />, respectively, should mark the extent 
    /// of the data which was previewed but not processed.
    /// </remarks>
    protected abstract void Consume(ref ReadOnlySequence<byte> buffer);
}