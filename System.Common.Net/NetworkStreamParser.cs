using System.Buffers;
using System.IO.Pipelines;
using System.Net.Transports;
using System.Threading;
using System.Threading.Tasks;
using static System.Threading.Tasks.Task;

namespace System.Net
{
    public abstract class NetworkStreamParser : AsyncConnectedObject
    {
        private readonly bool disposeTransport;
        private readonly NetworkTransport transport;
        private Pipe pipe;
        private Task processor;
        private CancellationTokenSource processorCts;

        protected NetworkStreamParser(NetworkTransport transport, bool disposeTransport)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            this.disposeTransport = disposeTransport;
        }

        protected override Task OnConnectAsync(CancellationToken cancellationToken)
        {
            return transport.ConnectAsync(cancellationToken);
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

                try
                {
                    await transport.DisconnectAsync().ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            }
        }

        protected abstract void ParseBuffer(in ReadOnlySequence<byte> buffer, out int consumed);

        protected override Task OnConnectedAsync(CancellationToken cancellationToken)
        {
            pipe = new Pipe(new PipeOptions(useSynchronizationContext: false));

            processorCts = new CancellationTokenSource();

            var token = processorCts.Token;

            processor = WhenAll(StartNetworkReaderAsync(pipe.Writer, token), StartParserAsync(pipe.Reader, token));

            return CompletedTask;
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
                // ignored
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
                // ignored
            }
            finally
            {
                reader.Complete();
            }
        }

        #region Overrides of AsyncConnectedObject

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if(disposing && disposeTransport)
            {
                transport.Dispose();
            }
        }

        #endregion
    }
}