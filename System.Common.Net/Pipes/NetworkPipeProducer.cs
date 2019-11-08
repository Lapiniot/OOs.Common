using System.IO.Pipelines;
using System.Net.Connections;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Properties.Strings;
using static System.Threading.LazyThreadSafetyMode;

namespace System.Net.Pipes
{
    /// <summary>
    /// Provides generic pipe data producer which reads data from abstract <seealso cref="INetworkConnection" />
    /// on data arrival and writes it to the pipe. Reads by consumers are supported via
    /// implemented <seealso cref="System.IO.Pipelines.PipeReader" /> methods.
    /// </summary>
    public sealed class NetworkPipeProducer : PipeReader, IAsyncDisposable
    {
        private readonly INetworkConnection connection;
        private readonly PipeOptions pipeOptions;
        private bool disposed;
        private CancellationTokenSource globalTokenSource;
        private PipeReader pipeReader;
        private PipeWriter pipeWriter;
        private Lazy<Task> producer;

        public NetworkPipeProducer(INetworkConnection connection, PipeOptions pipeOptions = null)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.pipeOptions = pipeOptions ?? new PipeOptions(useSynchronizationContext: false);
        }

        #region Implementation of IAsyncDisposable

        public async ValueTask DisposeAsync()
        {
            if(disposed) return;

            try
            {
                await StopAsync().ConfigureAwait(false);
            }
            finally
            {
                disposed = true;
            }
        }

        #endregion

        public void Start()
        {
            CheckDisposed();

            if(Interlocked.CompareExchange(ref producer, new Lazy<Task>(StartAsync, ExecutionAndPublication), null) == null)
            {
                var _ = producer.Value;
            }
        }

        public async ValueTask StopAsync()
        {
            var localProducer = producer;
            var localSource = globalTokenSource;

            if(localSource != null && localProducer != null)
            {
                try
                {
                    localSource.Cancel();
                    await localProducer.Value.ConfigureAwait(false);
                }
                finally
                {
                    if(Interlocked.CompareExchange(ref producer, null, localProducer) == localProducer)
                    {
                        localSource.Dispose();
                    }
                }
            }
        }

        private async Task StartAsync()
        {
            var tokenSource = new CancellationTokenSource();
            (pipeReader, pipeWriter) = new Pipe(pipeOptions);
            globalTokenSource = tokenSource;
            await StartProducerAsync(pipeWriter, tokenSource.Token).ConfigureAwait(false);
        }

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

                    var rt = connection.ReceiveAsync(buffer, token);
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
            catch(Exception exception)
            {
                writer.Complete(exception);
                throw;
            }
        }

        #region Overrides of PipeReader

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

        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            return pipeReader.ReadAsync(cancellationToken);
        }

        public override bool TryRead(out ReadResult result)
        {
            return pipeReader.TryRead(out result);
        }

        #endregion
    }
}