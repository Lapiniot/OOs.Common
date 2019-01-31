using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Listeners
{
    public abstract class AsyncAsyncConnectionListener : IAsyncConnectionListener
    {
        private CancellationTokenSource globalCts;

        public async IAsyncEnumerator<INetworkTransport> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            using var tokenSource = new CancellationTokenSource();

            if(Interlocked.CompareExchange(ref globalCts, tokenSource, null) != null) throw new ArgumentException("Enumeration is already in progress.");

            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, tokenSource.Token);

            try
            {
                await foreach(var t in GetAsyncEnumerable(linkedSource.Token).ConfigureAwait(false)) yield return t;
            }
            finally
            {
                Interlocked.Exchange(ref globalCts, null);
            }
        }

        protected abstract IAsyncEnumerable<INetworkTransport> GetAsyncEnumerable(CancellationToken cancellationToken);

        protected virtual void Dispose(bool disposing)
        {
            Volatile.Read(ref globalCts)?.Cancel();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}