using System.Buffers;
using System.Diagnostics.CodeAnalysis;
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
    [SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Type implements IAsyncDisposable instead")]
    public abstract class PipeConsumer : ConnectedObject
    {
        private readonly PipeReader reader;
        private CancellationTokenSource cancellationTokenSource;
        private Task consumer;

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
            cancellationTokenSource = new CancellationTokenSource();

            consumer = StartConsumerAsync(cancellationTokenSource.Token);

            return CompletedTask;
        }

        protected override async Task OnDisconnectAsync()
        {
            using(cancellationTokenSource)
            {
                cancellationTokenSource.Cancel();

                await consumer.ConfigureAwait(false);
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

                    if(result.IsCompleted || result.IsCanceled) break;

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
                }

                reader.Complete();
                OnCompleted();
            }
            catch(OperationCanceledException)
            {
                reader.Complete();
                OnCompleted();
            }
            catch(Exception exception)
            {
                reader.Complete(exception);
                OnCompleted(exception);
                throw;
            }
        }

        /// <summary>
        /// Method gets called every time new data is available.
        /// </summary>
        /// <param name="buffer">Sequence of linked buffers containing data produced by the pipe writer</param>
        /// <returns>
        /// Amount of bytes actually consumed by our implementation or <value>0</value>
        /// if no data can be consumed at the moment.
        /// </returns>
        protected abstract long Consume(in ReadOnlySequence<byte> buffer);

        /// <summary>
        /// Method gets called when consumer completed its work
        /// </summary>
        /// <param name="exception">Should exceptions occur, last value is passed</param>
        protected abstract void OnCompleted(Exception exception = null);
    }
}