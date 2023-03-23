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

        if (task.IsCompletedSuccessfully)
        {
            return;
        }

        void Continuation() => _ = task.Exception;
        task.ConfigureAwait(false).GetAwaiter().UnsafeOnCompleted(Continuation);
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

        if (task.IsCompletedSuccessfully)
        {
            return;
        }

        void Continuation()
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

        task.ConfigureAwait(false).GetAwaiter().UnsafeOnCompleted(Continuation);
    }
}