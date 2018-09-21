using System.Threading.Tasks;

namespace System.Threading
{
    public static class SemaphoreSlimExtensions
    {
        public static async Task<SemaphoreSlimLockScope> GetLockAsync(this SemaphoreSlim semaphoreSlim, CancellationToken cancellationToken)
        {
            await semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

            return new SemaphoreSlimLockScope(semaphoreSlim);
        }
    }

    public readonly struct SemaphoreSlimLockScope : IDisposable
    {
        private readonly SemaphoreSlim semaphoreSlim;

        internal SemaphoreSlimLockScope(SemaphoreSlim semaphoreSlim)
        {
            this.semaphoreSlim = semaphoreSlim;
        }

        #region Implementation of IDisposable

        public void Dispose()
        {
            semaphoreSlim.Release();
        }

        #endregion
    }
}