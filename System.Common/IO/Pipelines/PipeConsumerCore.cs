using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace System.IO.Pipelines;

public abstract class PipeConsumerCore : ActivityObject
{
    protected async Task StartConsumerAsync([NotNull] PipeReader reader, CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);

                if (result.IsCanceled)
                {
                    // Exit reader loop gracefully
                    break;
                }

                var buffer = result.Buffer;

                while (Consume(ref buffer))
                {
                    // TODO: better call reader.AdvanceTo with appropriate position here before throwing exception
                    cancellationToken.ThrowIfCancellationRequested();
                }

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
    /// <returns><see langword="true" /> if data was successfully consumed.</returns>
    /// <remarks>Implementations should update <paramref name="buffer" /> reference
    /// with unprocessed buffer reminder.</remarks>
    protected abstract bool Consume(ref ReadOnlySequence<byte> buffer);
}