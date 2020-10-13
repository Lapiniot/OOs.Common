using System.Threading.Tasks;

namespace System.Threading
{
    public class CancelableScope : IAsyncCancelable
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

        #region Implementation of IAsyncCancelable

        public bool IsCompleted => task.IsCompleted;

        public bool IsCanceled => task.IsCanceled;

        public Exception Exception => task.Exception;

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