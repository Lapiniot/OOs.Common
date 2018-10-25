using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Pipes
{
    public sealed class NetworkPipeReader : PipeReader, IAsyncConnectedObject, IDisposable
    {
        private readonly SemaphoreSlim semaphore;
        private readonly INetworkTransport transport;
        private bool disposed;
        private bool isConnected;
        private Pipe pipe;
        private Task processor;
        private CancellationTokenSource processorCts;

        public NetworkPipeReader(INetworkTransport transport)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            semaphore = new SemaphoreSlim(1);
        }

        public bool IsConnected => isConnected;

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            if (!isConnected)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    if (!isConnected)
                    {
                        pipe = new Pipe(new PipeOptions(useSynchronizationContext: false));

                        processorCts = new CancellationTokenSource();

                        var token = processorCts.Token;

                        processor = StartNetworkReaderAsync(pipe.Writer, token);

                        isConnected = true;
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }

        public async Task DisconnectAsync()
        {
            CheckDisposed();

            if (isConnected)
            {
                await semaphore.WaitAsync().ConfigureAwait(false);

                try
                {
                    if (isConnected)
                    {
                        using (processorCts)
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
                        }
                    }
                }
                finally
                {
                    isConnected = false;
                    semaphore.Release();
                }
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                DisconnectAsync().ContinueWith(t => { semaphore.Dispose(); });

                disposed = true;
            }
        }

        private void CheckConnected([CallerMemberName] string callerName = null)
        {
            if (!isConnected) throw new InvalidOperationException($"Cannot call '{callerName}' in disconnected state.");
        }

        private void CheckDisposed()
        {
            if (disposed) throw new InvalidOperationException("Cannot use this instance - has been already disposed.");
        }

        private async Task StartNetworkReaderAsync(PipeWriter writer, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var buffer = writer.GetMemory();

                    var received = await transport.ReceiveAsync(buffer, token).ConfigureAwait(false);

                    if (received == 0)
                    {
                        break;
                    }

                    writer.Advance(received);

                    var result = await writer.FlushAsync(token).ConfigureAwait(false);

                    if (result.IsCompleted) break;
                }

                writer.Complete();
            }
            catch (Exception exception)
            {
                writer.Complete(exception);
            }
        }

        public override void AdvanceTo(SequencePosition consumed)
        {
            pipe.Reader.AdvanceTo(consumed);
        }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            pipe.Reader.AdvanceTo(consumed, examined);
        }

        public override void CancelPendingRead()
        {
            pipe.Reader.CancelPendingRead();
        }

        public override void Complete(Exception exception = null)
        {
            pipe.Reader.Complete(exception);
        }

        public override void OnWriterCompleted(Action<Exception, object> callback, object state)
        {
            pipe.Reader.OnWriterCompleted(callback, state);
        }

        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            return pipe.Reader.ReadAsync(cancellationToken);
        }

        public override bool TryRead(out ReadResult result)
        {
            return pipe.Reader.TryRead(out result);
        }
    }
}