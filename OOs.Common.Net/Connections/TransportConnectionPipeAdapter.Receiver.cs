using System.IO.Pipelines;

#nullable enable

namespace OOs.Net.Connections;

public partial class TransportConnectionPipeAdapter
{
    private async Task StartReceiverAsync(PipeWriter writer, CancellationToken cancellationToken)
    {
        Exception? exception = null;
        try
        {
            while (true)
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
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            await writer.CompleteAsync(exception).ConfigureAwait(false);
        }
    }
}