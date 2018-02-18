using System.Threading.Tasks;
using static System.Threading.Tasks.Task;

namespace System.Threading
{
    public static class TaskExtensions
    {
        /// <summary>
        /// Adds non-blocking cancellable waiting support to the task that doesn't support cancellation natively
        /// </summary>
        /// <typeparam name="T">Result type</typeparam>
        /// <param name="task">Task to be awaited</param>
        /// <param name="cancellationToken">Cancellation token to signal about external cancellation</param>
        /// <returns>
        /// Wrapper task that either containes <paramref name="task" /> after completion or cancelled proxy task if external
        /// cancellation has been signaled via <paramref name="cancellationToken" />
        /// </returns>
        public static async Task<Task<T>> WaitAsync<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var completionSource = new TaskCompletionSource<T>();

            var registration = completionSource.Bind(cancellationToken);

            try
            {
                return await WhenAny(completionSource.Task, task).ConfigureAwait(false);
            }
            finally
            {
                registration.Dispose();

                completionSource.TrySetResult(default);
            }
        }

        /// <summary>
        /// Adds non-blocking cancellable waiting support to the task that doesn't support cancellation natively
        /// </summary>
        /// <param name="task">Task to be awaited</param>
        /// <param name="cancellationToken">Cancellation token to signal about external cancellation</param>
        /// <returns>
        /// Wrapper task that either containes <paramref name="task" /> after completion or cancelled proxy task if external
        /// cancellation has been signaled via <paramref name="cancellationToken" />
        /// </returns>
        public static async Task<Task> WaitAsync(this Task task, CancellationToken cancellationToken)
        {
            var completionSource = new TaskCompletionSource<bool>();

            var registration = completionSource.Bind(cancellationToken);

            try
            {
                return await WhenAny(completionSource.Task, task).ConfigureAwait(false);
            }
            finally
            {
                registration.Dispose();

                completionSource.TrySetResult(default);
            }
        }

        /// <summary>
        /// Adds non-blocking cancellable waiting support to the <paramref name="task" /> that doesn't support cancellation
        /// natively
        /// and unwraps its result on completion
        /// </summary>
        /// <param name="task">Task to be awaited</param>
        /// <param name="cancellationToken">Cancellation token to signal about external cancellation</param>
        /// <returns>
        /// Either original <paramref name="task" /> on completion or cancelled proxy task if cancellation has been
        /// requested via <paramref name="cancellationToken" />
        /// </returns>
        /// <exception cref="TaskCanceledException">If cancellation requested via <paramref name="cancellationToken" /></exception>
        public static async Task WaitAndUnwrapAsync(this Task task, CancellationToken cancellationToken)
        {
            await (await task.WaitAsync(cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
        }


        /// <summary>
        /// Adds non-blocking cancellable waiting support to the <paramref name="task" /> that doesn't support cancellation
        /// natively
        /// and unwraps its result on completion
        /// </summary>
        /// <typeparam name="T">Awaitable task result type</typeparam>
        /// <param name="task">Task to be awaited</param>
        /// <param name="cancellationToken">Cancellation token to signal about external cancellation</param>
        /// <returns>
        /// Either original <paramref name="task" /> on completion or cancelled proxy task if cancellation has been
        /// requested via <paramref name="cancellationToken" />
        /// </returns>
        /// <exception cref="TaskCanceledException">If cancellation requested via <paramref name="cancellationToken" /></exception>
        public static async Task<T> WaitAndUnwrapAsync<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            return await (await task.WaitAsync(cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
        }
    }
}