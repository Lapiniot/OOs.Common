using System.IO.Pipelines;

namespace OOs.Net.Connections;

public partial class TransportConnectionPipeAdapter
{
    private async Task RunReceiverAsync(PipeWriter writer)
    {
        Exception? exception = null;
        try
        {
            while (true)
            {
                var buffer = writer.GetMemory();

                var received = await ReceiveAsync(buffer).ConfigureAwait(false);

                if (received == 0)
                {
                    break;
                }

                writer.Advance(received);

                var result = await writer.FlushAsync().ConfigureAwait(false);

                if (result.IsCompleted || result.IsCanceled)
                {
                    break;
                }
            }
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