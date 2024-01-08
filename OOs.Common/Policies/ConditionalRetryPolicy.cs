namespace OOs.Policies;

public sealed class ConditionalRetryPolicy(IEnumerable<RepeatCondition> conditions) : RetryPolicy
{
    #region Overrides of RetryPolicy

    protected override bool ShouldRetry(Exception exception, int attempt, TimeSpan totalTime, ref TimeSpan delay)
    {
        foreach (var condition in conditions)
        {
            if (!condition(exception, attempt, totalTime, ref delay)) return false;
        }

        return true;
    }

    #endregion
}