using System.Collections.Generic;
using System.Net.Connections;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Listeners
{
    public abstract class ConnectionListener : IConnectionListener, IDisposable
    {
        private CancellationTokenSource globalCts;

        public async IAsyncEnumerator<INetworkConnection> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            using var tokenSource = new CancellationTokenSource();

            if(Interlocked.CompareExchange(ref globalCts, tokenSource, null) != null)
            {
                throw new ArgumentException("Enumeration is already in progress.");
            }

            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, tokenSource.Token);

            try
            {
                await foreach(var transport in GetAsyncEnumerable(linkedSource.Token).ConfigureAwait(false))
                {
                    yield return transport;
                }
            }
            finally
            {
                Interlocked.Exchange(ref globalCts, null);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract IAsyncEnumerable<INetworkConnection> GetAsyncEnumerable(CancellationToken cancellationToken);

        protected virtual void Dispose(bool disposing)
        {
            Volatile.Read(ref globalCts)?.Cancel();
        }
    }
}