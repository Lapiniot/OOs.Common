using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    public sealed class BlockingQueue<T> : IDisposable
    {
        private readonly ConcurrentQueue<T> queue;
        private readonly SemaphoreSlim semaphore;

        public BlockingQueue()
        {
            queue = new ConcurrentQueue<T>();
            semaphore = new SemaphoreSlim(0);
        }

        public void Enqueue(T item)
        {
            queue.Enqueue(item);
            semaphore.Release();
        }

        public async Task<(bool success, T result)> DequeueAsync(CancellationToken cancellationToken = default)
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            return (queue.TryDequeue(out var result), result);
        }

        public async Task<(bool success, T result)> DequeueAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            await semaphore.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
            return (queue.TryDequeue(out var result), result);
        }

        public async Task<(bool success, T result)> DequeueAsync(int millisecondsTimeout, CancellationToken cancellationToken = default)
        {
            await semaphore.WaitAsync(millisecondsTimeout, cancellationToken).ConfigureAwait(false);
            return (queue.TryDequeue(out var result), result);
        }

        #region IDisposable Support

        private bool disposed;

        public void Dispose()
        {
            if(!disposed)
            {
                semaphore.Dispose();
                disposed = true;
            }
        }

        #endregion
    }
}