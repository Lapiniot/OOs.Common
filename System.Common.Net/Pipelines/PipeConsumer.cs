using System.Buffers;
using System.IO.Pipelines;
using System.Net.Properties;
using System.Threading;
using System.Threading.Tasks;
using static System.Threading.Tasks.Task;

namespace System.Net.Pipelines
{
    /// <summary>
    /// Provides base abstract class for pipe data consumer.
    /// </summary>
    public abstract class PipeConsumer : ActivityObject
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
                if(!IsRunning) throw new InvalidOperationException(Strings.PipeNotStarted);

                return consumer;
            }
        }

        protected override Task StartingAsync(CancellationToken cancellationToken)
        {
            cancellationTokenSource = new CancellationTokenSource();

            consumer = StartConsumerAsync(cancellationTokenSource.Token);

            return CompletedTask;
        }

        protected override async Task StoppingAsync()
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
                    var result = rt.IsCompletedSuccessfully ? rt.Result : await rt.ConfigureAwait(false);

                    var buffer = result.Buffer;

                    if(buffer.Length > 0)
                    {
                        var shouldContinue = Consume(buffer, out var consumed);

                        if(consumed > 0)
                        {
                            reader.AdvanceTo(buffer.GetPosition(consumed));
                        }
                        else
                        {
                            reader.AdvanceTo(buffer.Start, buffer.End);
                        }

                        if(!shouldContinue) break;
                    }

                    if(result.IsCompleted || result.IsCanceled) break;
                }

                OnCompleted();
            }
            catch(OperationCanceledException)
            {
                OnCompleted();
            }
            catch(Exception exception) when(OnCompleted(exception))
            {
                // Suppress exception if OnCompleted returns true (handled as expected)
            }
        }

        /// <summary>
        /// Method gets called every time new data is available.
        /// </summary>
        /// <param name="sequence">Sequence of linked buffers containing data produced by the pipe writer</param>
        /// <param name="bytesConsumed">Amount of bytes actually consumed by our implementation or <value>0</value> if no data can be consumed at the moment.</param>
        /// <returns>
        /// <value>True</value> if consumer should continue reading, otherwise <value>False</value> to immidiately terminate processing 
        /// </returns>
        protected abstract bool Consume(in ReadOnlySequence<byte> sequence, out long bytesConsumed);

        /// <summary>
        /// Method gets called when consumer completed its work
        /// </summary>
        /// <param name="exception">Should exceptions occur, last value is passed</param>
        /// <returns><value>True</value> if exception is handled and shouldn't be rethrown</returns>
        protected abstract bool OnCompleted(Exception exception = null);
    }
}