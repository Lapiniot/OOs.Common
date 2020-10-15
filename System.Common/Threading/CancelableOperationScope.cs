using System.Threading.Tasks;

namespace System.Threading
{
    public class CancelableOperationScope : IAsyncCancelable
    {
        private readonly CancellationTokenSource jointCts;
        private readonly CancellationTokenSource localCts;

        private CancelableOperationScope(Func<CancellationToken, Task> operation, CancellationToken stoppingToken)
        {
            if(operation == null) throw new ArgumentNullException(nameof(operation));

            localCts = new CancellationTokenSource();

            var token = stoppingToken != default
                ? (jointCts = CancellationTokenSource.CreateLinkedTokenSource(localCts.Token, stoppingToken)).Token
                : localCts.Token;

            try
            {
                Completion = operation(token);
            }
            catch
            {
                using(localCts) using(jointCts) { };
                throw;
            }
        }

        public static CancelableOperationScope StartInScope(Func<CancellationToken, Task> operation, CancellationToken stoppingToken = default)
        {
            return new CancelableOperationScope(operation, stoppingToken);
        }

        #region Implementation of IAsyncCancelable

        bool IAsyncCancelable.IsCompleted => Completion.IsCompleted;

        bool IAsyncCancelable.IsCanceled => Completion.IsCanceled;

        Exception IAsyncCancelable.Exception => Completion.Exception;

        public Task Completion { get; }

        public async ValueTask DisposeAsync()
        {
            localCts.Cancel();

            using(localCts) using(jointCts)
            {
                try
                {
                    await Completion.ConfigureAwait(false);
                }
                catch(OperationCanceledException)
                {
                }
            }
        }

        #endregion
    }
}