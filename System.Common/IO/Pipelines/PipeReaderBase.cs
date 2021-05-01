using System.Properties;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipelines
{
    public abstract class PipeReaderBase : PipeReader, IAsyncDisposable
    {
        private const int Stopped = 0;
        private const int Started = 1;
        private const int Stopping = 2;
        private bool disposed;
        private CancellationTokenSource globalCts;
        private PipeReader pipeReader;
        private PipeWriter pipeWriter;
        private Task producer;
        private int stateGuard;

        protected PipeReaderBase(PipeOptions pipeOptions = null)
        {
            (pipeReader, pipeWriter) = new Pipe(pipeOptions ?? new PipeOptions(useSynchronizationContext: false));
        }

        #region Implementation of IAsyncDisposable

        public async ValueTask DisposeAsync()
        {
            if(disposed) return;

            GC.SuppressFinalize(this);

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

            switch(Interlocked.CompareExchange(ref stateGuard, Started, Stopped))
            {
                case Stopped:
                    var cts = new CancellationTokenSource();
                    globalCts = cts;
                    producer = StartProducerAsync(pipeWriter, cts.Token);
                    break;
                case Stopping:
                    throw new InvalidOperationException("Cannot start in this state, currently in stopping transition.");
            }
        }

        public async ValueTask StopAsync()
        {
            var localWorker = producer;
            var localCts = globalCts;

            switch(Interlocked.CompareExchange(ref stateGuard, Stopping, Started))
            {
                case Started:
                    // we are responsible for cancellation and cleanup
                    localCts.Cancel();
                    try
                    {
                        await localWorker.ConfigureAwait(false);
                    }
                    finally
                    {
                        localCts.Dispose();
                        Interlocked.Exchange(ref stateGuard, Stopped);
                    }

                    break;
                case Stopping:
                    // stopping in progress already, wait for currently active task in flight
                    await localWorker.ConfigureAwait(false);
                    break;
            }
        }

        private void CheckDisposed()
        {
            if(disposed) throw new InvalidOperationException(Strings.ObjectInstanceDisposed);
        }

        private async Task StartProducerAsync(PipeWriter writer, CancellationToken token)
        {
            try
            {
                while(!token.IsCancellationRequested)
                {
                    var buffer = writer.GetMemory();

                    var rt = ReceiveAsync(buffer, token);
                    var received = rt.IsCompletedSuccessfully ? rt.Result : await rt.ConfigureAwait(false);

                    if(received == 0) break;

                    writer.Advance(received);

                    var ft = writer.FlushAsync(token);
                    var result = ft.IsCompletedSuccessfully ? ft.Result : await ft.ConfigureAwait(false);

                    if(result.IsCompleted || result.IsCanceled) break;
                }

                await writer.CompleteAsync().ConfigureAwait(false);
            }
            catch(OperationCanceledException)
            {
                await writer.CompleteAsync().ConfigureAwait(false);
            }
            catch(Exception exception)
            {
                await writer.CompleteAsync(exception).ConfigureAwait(false);
                throw;
            }
        }

        protected abstract ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);

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