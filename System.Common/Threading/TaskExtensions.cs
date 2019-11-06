using System.Threading.Tasks;
using static System.Threading.Tasks.TaskContinuationOptions;
using static System.Threading.Tasks.TaskScheduler;

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
        /// <returns>Wrapper task</returns>
        public static Task<T> WaitAsync<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            if(task is null) throw new ArgumentNullException(nameof(task));

            return task.IsCompleted ? task : task.ContinueWith(t => t.GetAwaiter().GetResult(), cancellationToken, ExecuteSynchronously, Default);
        }

        /// <summary>
        /// Adds non-blocking cancellable waiting support to the task that doesn't support cancellation natively
        /// </summary>
        /// <param name="task">Task to be awaited</param>
        /// <param name="cancellationToken">Cancellation token to signal about external cancellation</param>
        /// <returns>Wrapper task</returns>
        public static Task WaitAsync(this Task task, CancellationToken cancellationToken)
        {
            if(task is null) throw new ArgumentNullException(nameof(task));

            return task.IsCompleted ? task : task.ContinueWith(t => t.GetAwaiter().GetResult(), cancellationToken, ExecuteSynchronously, Default);
        }
    }
}