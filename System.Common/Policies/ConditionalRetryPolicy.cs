namespace System.Policies;

public sealed class ConditionalRetryPolicy : RetryPolicy
{
    private readonly IEnumerable<RepeatCondition> conditions;

    public ConditionalRetryPolicy(IEnumerable<RepeatCondition> conditions) => this.conditions = conditions;

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