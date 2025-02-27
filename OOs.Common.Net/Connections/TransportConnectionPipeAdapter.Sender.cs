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
                    break;

                var buffer = result.Buffer;

                // TODO: Test hot path when sequence consists of single span for potential performance impact
                foreach (var chunk in buffer)
                {
                    await SendAsync(chunk, cancellationToken).ConfigureAwait(false);
                }

                reader.AdvanceTo(buffer.End, buffer.End);

                if (result.IsCompleted)
                    break;
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