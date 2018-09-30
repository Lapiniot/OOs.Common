namespace System.Policies
{
    public delegate bool RetryConditionHandler(Exception exception, int attempt, TimeSpan totalTime, ref TimeSpan delay);

    public class ConditionalRetryPolicy : RetryPolicy
    {
        private readonly RetryConditionHandler[] conditions;

        public ConditionalRetryPolicy()
        {
        }

        public ConditionalRetryPolicy(params RetryConditionHandler[] conditions)
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