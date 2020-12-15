﻿using System.Buffers;
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
                        var stop = !Consume(buffer, out var consumed);

                        if(consumed > 0)
                        {
                            reader.AdvanceTo(buffer.GetPosition(consumed));
                        }
                        else
                        {
                            reader.AdvanceTo(buffer.Start, buffer.End);
                        }

                        if(stop) break;
                    }

                    if(result.IsCompleted || result.IsCanceled) break;
                }

                OnCompleted();
            }
            catch(OperationCanceledException)
            {
                OnCompleted();
            }
            catch(Exception exception)
            {
                OnCompleted(exception);
                throw;
            }
        }

        /// <summary>
        /// Method gets called every time new data is available.
        /// </summary>
        /// <param name="buffer">Sequence of linked buffers containing data produced by the pipe writer</param>
        /// <param name="consumed">Amount of bytes actually consumed by our implementation or <value>0</value> if no data can be consumed at the moment.</param>
        /// <returns>
        /// <value>True</value> if consumer should continue reading, otherwise <value>False</value> to immidiately terminate processing 
        /// </returns>
        protected abstract bool Consume(in ReadOnlySequence<byte> buffer, out long consumed);

        /// <summary>
        /// Method gets called when consumer completed its work
        /// </summary>
        /// <param name="exception">Should exceptions occur, last value is passed</param>
        protected abstract void OnCompleted(Exception exception = null);
    }
}