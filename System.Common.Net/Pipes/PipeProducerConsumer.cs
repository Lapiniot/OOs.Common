﻿using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using static System.Threading.Tasks.Task;

namespace System.Net.Pipes
{
    /// <summary>
    /// Provides base pipe processor implementation which runs two loops.
    /// Producer loop reads data from the abstract source asynchronously on
    /// arrival via <see cref="ReceiveAsync" /> and writes to the pipe.
    /// Consumer loop consumes data from the pipe via <see cref="Consume" />.
    /// </summary>
    public abstract class PipeProducerConsumer : AsyncConnectedObject
    {
        private CancellationTokenSource cancellationTokenSource;
        private Task processor;

        protected override Task OnConnectAsync(CancellationToken cancellationToken)
        {
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            var pipe = new Pipe();

            var producer = StartProducerAsync(pipe.Writer, token);
            var consumer = StartConsumerAsync(pipe.Reader, token);
            processor = WhenAll(producer, consumer);

            return CompletedTask;
        }

        protected override async Task OnDisconnectAsync()
        {
            using(cancellationTokenSource)
            {
                cancellationTokenSource.Cancel();
                await processor.ConfigureAwait(false);
            }
        }

        private async Task StartProducerAsync(PipeWriter writer, CancellationToken cancellationToken)
        {
            try
            {
                while(!cancellationToken.IsCancellationRequested)
                {
                    var buffer = writer.GetMemory();

                    var rt = ReceiveAsync(buffer, cancellationToken);
                    var received = rt.IsCompletedSuccessfully ? rt.Result : await rt.AsTask().ConfigureAwait(false);

                    if(received == 0) break;

                    writer.Advance(received);

                    var ft = writer.FlushAsync(cancellationToken);
                    var result = ft.IsCompletedSuccessfully ? ft.Result : await ft.AsTask().ConfigureAwait(false);

                    if(result.IsCompleted || result.IsCanceled) break;
                }

                writer.Complete();
            }
            catch(OperationCanceledException)
            {
                writer.Complete();
            }
            catch(AggregateException age)
            {
                writer.Complete(age.GetBaseException());
            }
            catch(Exception exception)
            {
                writer.Complete(exception);
            }
        }

        private async Task StartConsumerAsync(PipeReader reader, CancellationToken cancellationToken)
        {
            try
            {
                while(!cancellationToken.IsCancellationRequested)
                {
                    var rt = reader.ReadAsync(cancellationToken);
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
        /// Provides abstract data reading support from asynchronous sources (network stream, socket e.g.)
        /// </summary>
        /// <param name="buffer">Memory buffer to be filled with data</param>
        /// <param name="cancellationToken"><see cref="CancellationToken" /> for external cancellation support.</param>
        /// <returns>Actual amount of the data received.</returns>
        protected abstract ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);

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