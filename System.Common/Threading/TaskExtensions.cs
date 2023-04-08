namespace System.Threading;

#pragma warning disable CA1031

public static class TaskExtensions
{
    /// <summary>
    /// Observes task errors on completion in order to avoid eventual unobserved task exceptions.
    /// Use this extension method if you simply need to fire-and-forget async methods.
    /// </summary>
    /// <param name="task">Task to be observed for errors</param>
    public static void Observe(this Task task)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (!task.IsCompletedSuccessfully)
        {
            var ctx = new DiscardErrorContinuationContext(task);
            task.ConfigureAwait(false).GetAwaiter().UnsafeOnCompleted(ctx.OnCompleted);
        }
    }

    /// <summary>
    /// Observes task errors on completion in order to avoid eventual unobserved task exceptions.
    /// Use this extension method if you simply need to fire-and-forget async methods.
    /// </summary>
    /// <param name="task">Task to be observed for errors</param>
    /// <param name="onError">Callback to be called when exception is observed</param> 
    public static void Observe(this Task task, Action<Exception> onError)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(onError);

        if (!task.IsCompletedSuccessfully)
        {
            var ctx = new ReportErrorContinuationContext(task, onError);
            task.ConfigureAwait(false).GetAwaiter().UnsafeOnCompleted(ctx.OnCompleted);
        }
    }

    private sealed class ReportErrorContinuationContext
    {
        private readonly Task task;
        private readonly Action<Exception> onError;

        public ReportErrorContinuationContext(Task task, Action<Exception> onError)
        {
            this.task = task;
            this.onError = onError;
        }

        public void OnCompleted()
        {
            if (task.Exception is { } ex)
            {
                try
                {
                    foreach (var e in ex.Flatten().InnerExceptions)
                    {
                        onError(e);
                    }
                }
                catch { /* Expected */ }
            }
        }
    }

    private sealed class DiscardErrorContinuationContext
    {
        private readonly Task task;

        public DiscardErrorContinuationContext(Task task) => this.task = task;

        public void OnCompleted() => _ = task.Exception;
    }
}