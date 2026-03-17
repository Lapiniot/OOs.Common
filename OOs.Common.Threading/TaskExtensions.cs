namespace OOs.Threading;

#pragma warning disable CA1034 // Nested types should not be visible
public static class TaskExtensions
{
    extension(Task task)
    {
        /// <summary>
        /// Observes task errors on completion in order to avoid eventual unobserved task exceptions.
        /// Use this extension method if you simply need to fire-and-forget async methods.
        /// </summary>
        public void Observe()
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
        /// <param name="onError">Callback to be called when exception is observed</param> 
        public void Observe(Action<Exception> onError)
        {
            ArgumentNullException.ThrowIfNull(task);
            ArgumentNullException.ThrowIfNull(onError);

            if (!task.IsCompletedSuccessfully)
            {
                var ctx = new ReportErrorContinuationContext(task, onError);
                task.ConfigureAwait(false).GetAwaiter().UnsafeOnCompleted(ctx.OnCompleted);
            }
        }
    }

    private sealed class ReportErrorContinuationContext(Task task, Action<Exception> onError)
    {
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
#pragma warning disable CA1031 // Do not catch general exception types
                catch { /* by design */ }
#pragma warning restore CA1031 // Do not catch general exception types
            }
        }
    }

    private sealed class DiscardErrorContinuationContext(Task task)
    {
        public void OnCompleted() => _ = task.Exception;
    }
}