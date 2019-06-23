using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Properties.Strings;

namespace System.Net.Pipes
{
    /// <summary>
    /// Provides generic pipe data producer which reads data from abstract <seealso cref="INetworkTransport" />
    /// on data arrival and writes it to the pipe. Reads by consumers are supported via
    /// implemented <seealso cref="System.IO.Pipelines.PipeReader" /> methods.
    /// </summary>
    public sealed class NetworkPipeProducer : PipeReader, IAsyncConnectedObject, IAsyncDisposable, IDisposable
    {
        private readonly PipeOptions pipeOptions;
        private readonly SemaphoreSlim semaphore;
        private readonly INetworkTransport transport;
        private bool disposed;
        private PipeReader pipeReader;
        private PipeWriter pipeWriter;
        private CancellationTokenSource processorCts;
        private Task producer;

        public NetworkPipeProducer(INetworkTransport transport, PipeOptions pipeOptions = null)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            semaphore = new SemaphoreSlim(1);
            this.pipeOptions = pipeOptions ?? new PipeOptions(useSynchronizationContext: false);
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
                        var pipe = new Pipe(pipeOptions);
                        pipeReader = pipe.Reader;
                        pipeWriter = pipe.Writer;

                        processorCts = new CancellationTokenSource();

                        var token = processorCts.Token;

                        producer = StartProducerAsync(pipeWriter, token);

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
                                pipeReader = null;
                                pipeWriter = null;

                                await producer.ConfigureAwait(false);
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

        #region Implementation of IAsyncDisposable

        public async ValueTask DisposeAsync()
        {
            if (!disposed)
            {
                try
                {
                    await DisconnectAsync().ConfigureAwait(false);
                }
                finally
                {
                    disposed = true;
                    semaphore.Dispose();
                }
            }
        }

        #endregion

        #region Implementation of IDisposable

        public void Dispose()
        {
            _ = DisposeAsync();
        }

        #endregion

        private void CheckDisposed()
        {
            if(disposed) throw new InvalidOperationException(ObjectInstanceDisposed);
        }

        private async Task StartProducerAsync(PipeWriter writer, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var buffer = writer.GetMemory();

                    var rt = transport.ReceiveAsync(buffer, token);
                    var received = rt.IsCompletedSuccessfully ? rt.Result : await rt.AsTask().ConfigureAwait(false);

                    if(received == 0) break;

                    writer.Advance(received);

                    var ft = writer.FlushAsync(token);
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

        public override void AdvanceTo(SequencePosition consumed)
        {
            pipeReader.AdvanceTo(consumed);
        }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            pipeReader.AdvanceTo(consumed, examined);
        }

        public override void CancelPendingRead()
        {
            pipeReader.CancelPendingRead();
        }

        public override void Complete(Exception exception = null)
        {
            pipeReader.Complete(exception);
        }

        [Obsolete]
        public override void OnWriterCompleted(Action<Exception, object> callback, object state)
        {
            pipeReader.OnWriterCompleted(callback, state);
        }

        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            return pipeReader.ReadAsync(cancellationToken);
        }

        public override bool TryRead(out ReadResult result)
        {
            return pipeReader.TryRead(out result);
        }
    }
}