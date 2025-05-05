using System.IO.Pipelines;

#nullable enable

namespace OOs.Net.Connections;

public partial class TransportConnectionPipeAdapter
{
    private async Task StartSenderAsync(PipeReader reader, CancellationToken cancellationToken)
    {
        Exception? exception = null;
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

                if (buffer.IsSingleSegment)
                {
                    await SendAsync(buffer.First, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var position = buffer.Start;
                    while (buffer.TryGet(ref position, out var segment))
                    {
                        await SendAsync(segment, cancellationToken).ConfigureAwait(false);
                    }
                }

                reader.AdvanceTo(consumed: buffer.End, examined: buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            await reader.CompleteAsync(exception).ConfigureAwait(false);
        }
    }
}