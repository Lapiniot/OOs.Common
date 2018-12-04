using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Pipes
{
    public abstract class BufferedDataProcessor : AsyncConnectedObject
    {
        private CancellationTokenSource cancellationTokenSource;
        private Task processor;

        protected override Task OnConnectAsync(CancellationToken cancellationToken)
        {
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            var pipe = new Pipe();
            processor = Task.WhenAll(StartReaderAsync(pipe.Writer, token), StartProcessorAsync(pipe.Reader, token));

            return Task.CompletedTask;
        }

        protected override async Task OnDisconnectAsync()
        {
            using(cancellationTokenSource)
            {
                cancellationTokenSource.Cancel();
                await processor.ConfigureAwait(false);
            }
        }

        private async Task StartReaderAsync(PipeWriter writer, CancellationToken cancellationToken)
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

        private async Task StartProcessorAsync(PipeReader reader, CancellationToken cancellationToken)
        {
            try
            {
                while(!cancellationToken.IsCancellationRequested)
                {
                    var rt = reader.ReadAsync(cancellationToken);
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

        protected abstract ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);

        protected abstract long Process(in ReadOnlySequence<byte> buffer);
    }
}