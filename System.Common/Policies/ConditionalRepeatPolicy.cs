namespace System.Policies;

public delegate bool RepeatCondition(Exception exception, int attempt, TimeSpan totalTime, ref TimeSpan delay);

public class ConditionalRepeatPolicy : RepeatPolicy
{
    private readonly IEnumerable<RepeatCondition> conditions;

    public ConditionalRepeatPolicy(IEnumerable<RepeatCondition> conditions) => this.conditions = conditions;

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