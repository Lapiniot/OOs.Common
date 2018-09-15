using System.Buffers;
using System.IO.Pipelines;
using System.Net.Transports;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    public abstract class NetworkStreamParser<TOptions> : AsyncConnectedObject<TOptions>
    {
        private readonly NetworkTransport transport;
        private Pipe pipe;
        private Task processor;
        private CancellationTokenSource processorCts;

        protected NetworkStreamParser(NetworkTransport transport)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        protected override Task OnConnectAsync(TOptions options, CancellationToken cancellationToken)
        {
            return transport.ConnectAsync(null, cancellationToken);
        }

        protected override async Task OnCloseAsync()
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

                try
                {
                    await transport.CloseAsync().ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            }
        }

        protected abstract void ParseBuffer(in ReadOnlySequence<byte> buffer, out int consumed);

        protected override Task OnConnectedAsync(TOptions options, CancellationToken cancellationToken)
        {
            pipe = new Pipe(new PipeOptions(useSynchronizationContext: false));

            processorCts = new CancellationTokenSource();

            var token = processorCts.Token;

            processor = Task.WhenAll(StartNetworkReaderAsync(pipe.Writer, token), StartParserAsync(pipe.Reader, token));

            return Task.CompletedTask;
        }

        protected Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            return transport.ReceiveAsync(buffer, cancellationToken);
        }

        protected Task<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            return transport.SendAsync(buffer, cancellationToken);
        }

        protected abstract void OnEndOfStream();

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
            }
            catch(OperationCanceledException)
            {
            }
            finally
            {
                writer.Complete();
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
            }
            catch(OperationCanceledException)
            {
            }
            finally
            {
                reader.Complete();
            }
        }
    }
}