using System.Threading.Tasks;

namespace System.Threading
{
    public class CancellationScope : IAsyncDisposable
    {
        private readonly CancellationTokenSource jointCts;
        private readonly CancellationTokenSource localCts;
        private readonly Task task;

        public CancellationScope(Func<CancellationToken, Task> taskFactory, CancellationToken externalToken)
        {
            if(taskFactory == null) throw new ArgumentNullException(nameof(taskFactory));

            localCts = new CancellationTokenSource();
            jointCts = CancellationTokenSource.CreateLinkedTokenSource(localCts.Token, externalToken);

            task = taskFactory(jointCts.Token);
        }

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