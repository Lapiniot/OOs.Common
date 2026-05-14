using System.IO.Pipelines;

namespace OOs.Net.Connections;

public partial class TransportConnectionPipeAdapter
{
    private async Task RunSenderAsync(PipeReader reader)
    {
        Exception? exception = null;
        try
        {
            while (true)
            {
                var result = await reader.ReadAsync().ConfigureAwait(false);

                if (result.IsCanceled)
                {
                    break;
                }

                var buffer = result.Buffer;
                await SendAsync(ref buffer).ConfigureAwait(false);

                reader.AdvanceTo(consumed: buffer.End, examined: buffer.End);

                if (result.IsCompleted)
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
            await reader.CompleteAsync(exception).ConfigureAwait(false);
        }
    }
}