using System.Collections.Generic;
using System.Net.Connections;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Listeners
{
    public abstract class ConnectionListener : IConnectionListener
    {
        private const int Running = 1;
        private const int Stopped = 0;
        private int state;

        public async IAsyncEnumerator<INetworkConnection> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            if(Interlocked.CompareExchange(ref state, Running, Stopped) != Stopped)
            {
                throw new ArgumentException("Enumeration is already in progress.");
            }

            try
            {
                await foreach(var transport in GetAsyncEnumerable(cancellationToken).ConfigureAwait(false))
                {
                    yield return transport;
                }
            }
            finally
            {
                Interlocked.Exchange(ref state, Stopped);
            }
        }

        protected abstract IAsyncEnumerable<INetworkConnection> GetAsyncEnumerable(CancellationToken cancellationToken);
    }
}