using System.Collections.Generic;

namespace System.Policies
{
    public delegate bool RetryCondition(Exception exception, int attempt, TimeSpan totalTime, ref TimeSpan delay);

    public class ConditionalRetryPolicy : RetryPolicy
    {
        private readonly IEnumerable<RetryCondition> conditions;

        public ConditionalRetryPolicy(IEnumerable<RetryCondition> conditions)
        {
            this.conditions = conditions;
        }

        #region Overrides of RetryPolicy

        protected override bool ShouldRetry(Exception exception, int attempt, TimeSpan totalTime, ref TimeSpan delay)
        {
            foreach(var condition in conditions)
            {
                if(!condition(exception, attempt, totalTime, ref delay)) return false;
            }

            return true;
        }

        #endregion
    }
}