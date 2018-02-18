using System.Threading.Tasks;

namespace System.Threading
{
    public abstract class AsyncEnumerator<T> : IDisposable
    {
        public abstract void Dispose();

        public abstract Task<T> GetNextAsync(CancellationToken cancellationToken);
    }
}