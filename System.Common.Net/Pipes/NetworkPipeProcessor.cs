using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Pipes
{
    public abstract class NetworkPipeProcessor : AsyncConnectedObject
    {
        private readonly NetworkPipeReader reader;
        private Task processor;
        private CancellationTokenSource processorCts;

        protected NetworkPipeProcessor(NetworkPipeReader reader)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        protected override Task OnConnectAsync(CancellationToken cancellationToken)
        {
            processorCts = new CancellationTokenSource();

            processor = StartParserAsync(processorCts.Token);

            return Task.CompletedTask;
        }

        protected override async Task OnDisconnectAsync()
        {
            using(processorCts)
            {
                processorCts.Cancel();

                try
                {
                    await processor.ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            }
        }

        private async Task StartParserAsync(CancellationToken token)
        {
            try
            {
                while(!token.IsCancellationRequested)
                {
                    var readTask = reader.ReadAsync(token);

                    var result = readTask.IsCompletedSuccessfully
                        ? readTask.Result
                        : await readTask.ConfigureAwait(false);

                    var buffer = result.Buffer;

                    if(buffer.IsEmpty) continue;

                    ParseBuffer(buffer, out var consumed);

                    if(consumed > 0)
                    {
                        reader.AdvanceTo(buffer.GetPosition(consumed));
                    }
                    else
                    {
                        reader.AdvanceTo(buffer.Start, buffer.End);
                    }

                    if(result.IsCompleted || result.IsCanceled) break;
                }

                reader.Complete();
            }
            catch(Exception exception)
            {
                reader.Complete(exception);
            }
        }

        protected abstract void ParseBuffer(in ReadOnlySequence<byte> buffer, out int consumed);
    }
}