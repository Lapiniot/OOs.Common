using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    public abstract class ConnectionListener : IConnectionListener
    {
        private IAsyncEnumerator<INetworkTransport> asyncEnumerator;
        private CancellationTokenSource globalCancellationTokenSource;

        public IAsyncEnumerator<INetworkTransport> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            var tokenSource = new CancellationTokenSource();

            try
            {
                if(Interlocked.CompareExchange(ref globalCancellationTokenSource, tokenSource, null) != null)
                {
                    throw new InvalidOperationException("Already accepting connections - enumeration is in progress.");
                }

                asyncEnumerator = StartListeningAsync(cancellationToken, tokenSource.Token);

                return asyncEnumerator;
            }
            catch
            {
                tokenSource.Dispose();
                Interlocked.Exchange(ref globalCancellationTokenSource, null);
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            using var tokenSource = Volatile.Read(ref globalCancellationTokenSource);

            if(tokenSource != null)
            {
                tokenSource.Cancel();
                try
                {
                    await asyncEnumerator.MoveNextAsync().ConfigureAwait(false);
                }
                finally
                {
                    Interlocked.Exchange(ref globalCancellationTokenSource, null);
                }
            }
        }

        private async IAsyncEnumerator<INetworkTransport> StartListeningAsync(CancellationToken externalToken, CancellationToken internalToken)
        {
            OnStartListening();

            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(externalToken, internalToken);

            try
            {
                var token = linkedSource.Token;
                while(!token.IsCancellationRequested)
                {
                    yield return await AcceptAsync(token).ConfigureAwait(false);
                }
            }
            finally
            {
                OnStopListening();
            }
        }

        public abstract Task<INetworkTransport> AcceptAsync(CancellationToken cancellationToken);

        protected abstract void OnStartListening();

        protected abstract void OnStopListening();
    }
}