using System.Buffers;
using System.IO.Pipelines;
using System.Net.Transports.Exceptions;
using System.Threading;
using System.Threading.Tasks;
using static System.Threading.Tasks.Task;

namespace System.Net
{
    public abstract class NetworkStreamParser : ConnectedObject
    {
        private readonly INetworkConnection connection;
        private Pipe pipe;
        private Task processor;
        private CancellationTokenSource processorCts;

        protected NetworkStreamParser(INetworkConnection connection)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        protected INetworkConnection Connection => connection;

        protected override Task OnConnectAsync(CancellationToken cancellationToken)
        {
            pipe = new Pipe(new PipeOptions(useSynchronizationContext: false));

            processorCts = new CancellationTokenSource();

            var token = processorCts.Token;

            processor = WhenAll(StartNetworkReaderAsync(pipe.Writer, token), StartParserAsync(pipe.Reader, token));

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
                catch(OperationCanceledException)
                {
                }
            }
        }

        protected abstract void ParseBuffer(in ReadOnlySequence<byte> buffer, out int consumed);

        protected async Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            try
            {
                return await connection.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
            }
            catch(ConnectionAbortedException)
            {
                OnConnectionAborted();
                throw;
            }
        }

        protected async Task<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            try
            {
                return await connection.SendAsync(buffer, cancellationToken).ConfigureAwait(false);
            }
            catch(ConnectionAbortedException)
            {
                OnConnectionAborted();
                throw;
            }
        }

        protected abstract void OnEndOfStream();

        protected abstract void OnConnectionAborted();

        private async Task StartNetworkReaderAsync(PipeWriter writer, CancellationToken token)
        {
            try
            {
                while(!token.IsCancellationRequested)
                {
                    var buffer = writer.GetMemory();

                    var received = await ReceiveAsync(buffer, token).ConfigureAwait(false);

                    if(received == 0)
                    {
                        OnEndOfStream();
                        break;
                    }

                    writer.Advance(received);

                    var result = await writer.FlushAsync(token).ConfigureAwait(false);

                    if(result.IsCompleted) break;
                }

                writer.Complete();
            }
            catch(OperationCanceledException)
            {
                writer.Complete();
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
                    var result = await reader.ReadAsync(token).ConfigureAwait(false);

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