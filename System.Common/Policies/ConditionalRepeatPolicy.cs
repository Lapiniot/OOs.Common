namespace System.Policies;

public delegate bool RepeatCondition(Exception exception, int attempt, TimeSpan totalTime, ref TimeSpan delay);

public sealed class ConditionalRepeatPolicy(IEnumerable<RepeatCondition> conditions) : RepeatPolicy
{
    #region Overrides of RetryPolicy

    protected override bool ShouldRepeat(Exception exception, int attempt, TimeSpan totalTime, ref TimeSpan delay)
    {
        foreach (var condition in conditions)
        {
            if (!condition(exception, attempt, totalTime, ref delay)) return false;
        }

        return true;
    }

    #endregion
}