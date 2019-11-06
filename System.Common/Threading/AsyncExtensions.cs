using System.Threading.Tasks;
using static System.Array;

namespace System.Threading
{
    public static class AsyncExtensions
    {
        public static IDisposable Bind<T>(this CancellationToken cancellationToken,
            TaskCompletionSource<T> completionSource)
        {
            return cancellationToken.Register(() => completionSource.TrySetCanceled(cancellationToken));
        }

        public static IDisposable Bind<T>(this CancellationToken cancellationToken,
            TaskCompletionSource<T> completionSource, Action cancelCallback)
        {
            return cancellationToken.Register(() => DoCancel(completionSource, cancelCallback, cancellationToken));
        }

        public static IDisposable Bind<T>(this TaskCompletionSource<T> completionSource,
            CancellationToken cancellationToken)
        {
            return cancellationToken.Register(() => completionSource.TrySetCanceled(cancellationToken));
        }

        public static IDisposable Bind<T>(this TaskCompletionSource<T> completionSource, Action cancelCallback, CancellationToken cancellationToken)
        {
            return cancellationToken.Register(() => DoCancel(completionSource, cancelCallback, cancellationToken));
        }

        private static void DoCancel<T>(TaskCompletionSource<T> completionSource, Action cancelCallback, CancellationToken cancellationToken)
        {
            completionSource.TrySetCanceled(cancellationToken);
            cancelCallback?.Invoke();
        }

        public static IDisposable Bind<T>(this TaskCompletionSource<T> completionSource,
            params CancellationToken[] tokens)
        {
            return new DisposeContainer(ConvertAll(tokens, t => (IDisposable)t.Register(() => completionSource.TrySetCanceled(t))));
        }

        private sealed class DisposeContainer : IDisposable
        {
            private readonly IDisposable[] targets;

            internal DisposeContainer(params IDisposable[] targets)
            {
                this.targets = targets;
            }

            public void Dispose()
            {
                foreach(var target in targets)
                {
                    using(target) { }
                }
            }
        }
    }
}