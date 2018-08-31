using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    public abstract class NetworkStreamParser<TOptions> : AsyncConnectedObject<TOptions>
    {
        private Pipe pipe;
        private Task processor;
        private CancellationTokenSource processorCts;
        protected Socket Socket;

        protected NetworkStreamParser(IPEndPoint endpoint)
        {
            Endpoint = endpoint;
        }

        public IPEndPoint Endpoint { get; }

        protected override async Task OnConnectAsync(TOptions options, CancellationToken cancellationToken)
        {
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await Socket.ConnectAsync(Endpoint).ConfigureAwait(false);
        }

        protected override async Task OnCloseAsync()
        {
            processorCts.Cancel();
            await processor.ConfigureAwait(false);

            Socket.Disconnect(false);
            Socket.Shutdown(SocketShutdown.Both);
            Socket.Close();
        }

        protected abstract void ParseBuffer(in ReadOnlySequence<byte> buffer, out int consumed);

        protected override Task OnConnectedAsync(TOptions options, CancellationToken cancellationToken)
        {
            pipe = new Pipe(new PipeOptions(minimumSegmentSize: 512));
            processorCts = new CancellationTokenSource();
            var token = processorCts.Token;
            processor = Task.WhenAll(
                Task.Run(() => StartNetworkReader(pipe.Writer, token), token),
                Task.Run(() => StartParser(pipe.Reader, token), token));

            return Task.CompletedTask;
        }

        private async Task StartNetworkReader(PipeWriter writer, CancellationToken token)
        {
            try
            {
                while(!token.IsCancellationRequested)
                {
                    var buffer = writer.GetMemory();

                    var received = await Socket.ReceiveAsync(buffer, SocketFlags.None, token).ConfigureAwait(false);

                    if(received == 0) break;

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

        private async Task StartParser(PipeReader reader, CancellationToken token)
        {
            try
            {
                while(!token.IsCancellationRequested)
                {
                    var result = await reader.ReadAsync(token).ConfigureAwait(false);

                    var buffer = result.Buffer;

                    if(buffer.IsEmpty) continue;

                    ParseBuffer(buffer, out var consumed);

                    reader.AdvanceTo(buffer.GetPosition(consumed));

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