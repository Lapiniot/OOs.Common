using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Net.Connections;
using System.Net.Connections.Exceptions;
using System.Threading;
using System.Threading.Tasks;
using static System.Threading.Tasks.Task;

namespace System.Net
{
    [SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Type implements IAsyncDisposable instead")]
    public abstract class NetworkStreamParser : ActivityObject
    {
        private readonly INetworkConnection connection;
        private CancellationTokenSource cancellationTokenSource;
        private Pipe pipe;
        private Task processor;

        protected NetworkStreamParser(INetworkConnection connection)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        protected INetworkConnection Connection => connection;

        protected override Task StartingAsync(CancellationToken cancellationToken)
        {
            pipe = new Pipe(new PipeOptions(useSynchronizationContext: false));

            cancellationTokenSource = new CancellationTokenSource();

            var token = cancellationTokenSource.Token;

            processor = WhenAll(StartReaderAsync(pipe.Writer, token), StartParserAsync(pipe.Reader, token));

            return CompletedTask;
        }

        protected override async Task StoppingAsync()
        {
            using(cancellationTokenSource)
            {
                cancellationTokenSource.Cancel();

                await processor.ConfigureAwait(false);
            }
        }

        protected abstract void ParseBuffer(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract void OnEndOfStream();

        protected abstract void OnConnectionAborted();

        private async Task StartReaderAsync(PipeWriter writer, CancellationToken token)
        {
            try
            {
                while(!token.IsCancellationRequested)
                {
                    var buffer = writer.GetMemory();

                    var rt = connection.ReceiveAsync(buffer, token);

                    var received = rt.IsCompletedSuccessfully ? rt.Result : await rt.ConfigureAwait(false);

                    if(received == 0)
                    {
                        OnEndOfStream();
                        break;
                    }

                    writer.Advance(received);

                    var ft = writer.FlushAsync(token);

                    var result = ft.IsCompletedSuccessfully ? ft.Result : await ft.ConfigureAwait(false);

                    if(result.IsCompleted) break;
                }

                writer.Complete();
            }
            catch(OperationCanceledException)
            {
                writer.Complete();
            }
            catch(ConnectionAbortedException cae)
            {
                writer.Complete(cae);
                OnConnectionAborted();
                throw;
            }
            catch(Exception exception)
            {
                writer.Complete(exception);
                throw;
            }
        }

        private async Task StartParserAsync(PipeReader reader, CancellationToken token)
        {
            try
            {
                while(!token.IsCancellationRequested)
                {
                    var rt = reader.ReadAsync(token);

                    var result = rt.IsCompletedSuccessfully ? rt.Result : await rt.ConfigureAwait(false);

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

                    if(result.IsCompleted) break;
                }

                reader.Complete();
            }
            catch(OperationCanceledException)
            {
                reader.Complete();
            }
            catch(Exception exception)
            {
                reader.Complete(exception);
                throw;
            }
        }
    }
}