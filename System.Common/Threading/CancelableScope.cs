using System.Threading.Tasks;

namespace System.Threading
{
    public class CancelableScope : IAsyncDisposable
    {
        private readonly CancellationTokenSource jointCts;
        private readonly CancellationTokenSource localCts;
        private readonly Task task;

        private CancelableScope(Func<CancellationToken, Task> taskFactory, CancellationToken externalToken)
        {
            if(taskFactory == null) throw new ArgumentNullException(nameof(taskFactory));

            localCts = new CancellationTokenSource();
            jointCts = CancellationTokenSource.CreateLinkedTokenSource(localCts.Token, externalToken);

            task = taskFactory(jointCts.Token);
        }

        public static CancelableScope StartInScope(Func<CancellationToken, Task> taskFactory, CancellationToken externalToken = default)
        {
            return new CancelableScope(taskFactory, externalToken);
        }

        public Task Completion => task;

        #region Implementation of IAsyncDisposable

        public async ValueTask DisposeAsync()
        {
            localCts.Cancel();

            using(jointCts)
            using(localCts)
            {
                await task.ConfigureAwait(false);
            }
        }

        #endregion
    }
}