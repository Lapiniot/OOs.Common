namespace System.Threading;

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

        // this method never throws, so no need to observe exceptions here :)
        _ = ObserveAsync(task);
    }

    private static async Task ObserveAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
#pragma warning disable CA1031
        catch
#pragma warning restore CA1031
        {
            // Expected, don't rethrow
        }
    }
}