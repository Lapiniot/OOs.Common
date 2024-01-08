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
                    break;

                var buffer = result.Buffer;

                while (!cancellationToken.IsCancellationRequested && Consume(ref buffer))
                {
                }

                reader.AdvanceTo(buffer.Start, buffer.End);

                cancellationToken.ThrowIfCancellationRequested();

                if (result.IsCompleted)
                    break;
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