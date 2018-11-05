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
        private Pipe pipe;
        private Task processor;
        private CancellationTokenSource processorCts;

        public NetworkPipeReader(INetworkTransport transport)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            semaphore = new SemaphoreSlim(1);
        }

        public bool IsConnected { get; private set; }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            if(!IsConnected)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    if(!IsConnected)
                    {
                        pipe = new Pipe(new PipeOptions(useSynchronizationContext: false));

                        processorCts = new CancellationTokenSource();

                        var token = processorCts.Token;

                        processor = StartNetworkReaderAsync(pipe.Writer, token);

                        IsConnected = true;
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

            if(IsConnected)
            {
                await semaphore.WaitAsync().ConfigureAwait(false);

                try
                {
                    if(IsConnected)
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
                    IsConnected = false;
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
            if(!IsConnected) throw new InvalidOperationException($"Cannot call '{callerName}' in disconnected state.");
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

                    var rt = transport.ReceiveAsync(buffer, token);
                    var received = rt.IsCompleted ? rt.Result : await rt.ConfigureAwait(false);

                    if(received == 0) break;

                    writer.Advance(received);

                    var ft = writer.FlushAsync(token);
                    var result = ft.IsCompleted ? ft.Result : await ft.ConfigureAwait(false);

                    if(result.IsCompleted || result.IsCanceled) break;
                }

                writer.Complete();
            }
            catch(AggregateException agge)
            {
                writer.Complete(agge.GetBaseException());
            }
            catch(Exception exception)
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