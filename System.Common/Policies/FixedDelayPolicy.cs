namespace System.Policies
{
    public class FixedDelayPolicy : RetryPolicy
    {
        private readonly TimeSpan delay;

        public FixedDelayPolicy(TimeSpan delay)
        {
            this.delay = delay;
        }

        #region Overrides of RetryPolicy

        protected override bool ShouldRetry(int attempt, TimeSpan totalTime, ref TimeSpan delay)
        {
            delay = this.delay;
            return false;
        }

        #endregion
    }
}