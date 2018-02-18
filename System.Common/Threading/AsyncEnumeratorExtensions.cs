using System.Threading.Tasks;

namespace System.Threading
{
    public static class AsyncEnumeratorExtensions
    {
        public static AsyncEnumerator<T2> Map<T1, T2>(this AsyncEnumerator<T1> enumerator, Func<T1, T2> mapFunc)
        {
            if(enumerator == null) throw new ArgumentNullException(nameof(enumerator));
            if(mapFunc == null) throw new ArgumentNullException(nameof(mapFunc));

            return new AsyncEnumeratorWrapper<T1, T2>(enumerator, mapFunc);
        }

        private class AsyncEnumeratorWrapper<T1, T2> : AsyncEnumerator<T2>
        {
            private readonly AsyncEnumerator<T1> enumerator;
            private readonly Func<T1, T2> mapper;

            public AsyncEnumeratorWrapper(AsyncEnumerator<T1> asyncEnumerator, Func<T1, T2> mapperFunc)
            {
                enumerator = asyncEnumerator;
                mapper = mapperFunc;
            }

            #region Overrides of AsyncEnumerator<T1>

            public override void Dispose()
            {
                enumerator.Dispose();
            }

            public override async Task<T2> GetNextAsync(CancellationToken cancellationToken)
            {
                return mapper(await enumerator.GetNextAsync(cancellationToken).ConfigureAwait(false));
            }

            #endregion
        }
    }
}