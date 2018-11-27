using System.Buffers;
using System.Net.Properties;
using System.Threading;
using System.Threading.Tasks;
using static System.Threading.Tasks.Task;

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

        public Task Completion
        {
            get
            {
                if(!IsConnected) throw new InvalidOperationException(Strings.PipeNotStarted);

                return processor;
            }
        }

        protected override Task OnConnectAsync(CancellationToken cancellationToken)
        {
            processorCts = new CancellationTokenSource();

            processor = StartProcessorAsync(processorCts.Token);

            return CompletedTask;
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

        private async Task StartProcessorAsync(CancellationToken token)
        {
            try
            {
                while(!token.IsCancellationRequested)
                {
                    var rt = reader.ReadAsync(token);
                    var result = rt.IsCompletedSuccessfully ? rt.Result : await rt.AsTask().ConfigureAwait(false);

                    var buffer = result.Buffer;

                    if(buffer.IsEmpty) continue;

                    var consumed = Process(buffer);

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
            catch(OperationCanceledException)
            {
                reader.Complete();
            }
            catch(AggregateException age)
            {
                reader.Complete(age.GetBaseException());
            }
            catch(Exception ex)
            {
                reader.Complete(ex);
            }
        }

        protected abstract int Process(ReadOnlySequence<byte> buffer);
    }
}