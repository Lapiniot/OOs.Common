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
                    var rt = reader.ReadAsync(token);
                    var result = rt.IsCompletedSuccessfully ? rt.Result : await rt.AsTask().ConfigureAwait(false);

                    var buffer = result.Buffer;

                    if(buffer.IsEmpty) continue;

                    var pt = ProcessAsync(buffer, token);
                    var consumed = pt.IsCompletedSuccessfully ? pt.Result : await pt.AsTask().ConfigureAwait(false);

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
            catch(AggregateException age)
            {
                reader.Complete(age.GetBaseException());
            }
            catch(Exception exception)
            {
                reader.Complete(exception);
            }
        }

        protected abstract ValueTask<int> ProcessAsync(ReadOnlySequence<byte> buffer, CancellationToken token);
    }
}