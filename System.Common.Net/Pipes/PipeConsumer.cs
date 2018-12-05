using System.Buffers;
using System.IO.Pipelines;
using System.Net.Properties;
using System.Threading;
using System.Threading.Tasks;
using static System.Threading.Tasks.Task;

namespace System.Net.Pipes
{
    /// <summary>
    /// Provides base abstract class for pipe data consumer.
    /// </summary>
    public abstract class PipeConsumer : AsyncConnectedObject
    {
        private readonly PipeReader reader;
        private Task consumer;
        private CancellationTokenSource processorCts;

        protected PipeConsumer(PipeReader reader)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        public Task Completion
        {
            get
            {
                if(!IsConnected) throw new InvalidOperationException(Strings.PipeNotStarted);

                return consumer;
            }
        }

        protected override Task OnConnectAsync(CancellationToken cancellationToken)
        {
            processorCts = new CancellationTokenSource();

            consumer = StartConsumerAsync(processorCts.Token);

            return CompletedTask;
        }

        protected override async Task OnDisconnectAsync()
        {
            using(processorCts)
            {
                processorCts.Cancel();

                try
                {
                    await consumer.ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            }
        }

        private async Task StartConsumerAsync(CancellationToken token)
        {
            try
            {
                while(!token.IsCancellationRequested)
                {
                    var rt = reader.ReadAsync(token);
                    var result = rt.IsCompletedSuccessfully ? rt.Result : await rt.AsTask().ConfigureAwait(false);

                    var buffer = result.Buffer;

                    if(buffer.IsEmpty) continue;

                    var consumed = Consume(buffer);

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

        /// <summary>
        /// The only method to be implemented. It is called every time new data is available.
        /// </summary>
        /// <param name="buffer">Sequence of linked buffers containing data produced by the pipe writer</param>
        /// <returns>
        /// Amount of bytes actually consumed by our implementation or <value>0</value>
        /// if no data can be consumed at the moment.
        /// </returns>
        protected abstract long Consume(in ReadOnlySequence<byte> buffer);
    }
}