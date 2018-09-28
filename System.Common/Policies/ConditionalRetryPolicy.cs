namespace System.Policies
{
    public delegate bool RetryConditionHandler(int attempt, TimeSpan totalTime, ref TimeSpan delay);

    public class ConditionalRetryPolicy : RetryPolicy
    {
        private readonly RetryConditionHandler[] conditions;

        public ConditionalRetryPolicy(RetryConditionHandler[] conditions)
        {
            this.conditions = conditions;
        }

        #region Overrides of RetryPolicy

        protected override bool ShouldRetry(int attempt, TimeSpan totalTime, ref TimeSpan delay)
        {
            foreach(var condition in conditions)
            {
                if(!condition(attempt, totalTime, ref delay)) return false;
            }

            return true;
        }

        #endregion
    }
}