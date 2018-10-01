﻿using System.Collections.Generic;
using static System.TimeSpan;

namespace System.Policies
{
    public class RetryPolicyBuilder
    {
        private readonly List<RetryConditionHandler> conditions;

        public RetryPolicyBuilder()
        {
            conditions = new List<RetryConditionHandler>();
        }

        /// <summary>
        /// Creates new instance of the retry policy
        /// </summary>
        /// <returns>New instance of the policy</returns>
        public IRetryPolicy Build()
        {
            return new ConditionalRetryPolicy(conditions.ToArray());
        }

        /// <summary>
        /// Appends custom retry condition handler to the current instance of the builder
        /// </summary>
        /// <param name="condition">Condition to add</param>
        /// <returns>Current instance of the builder</returns>
        public RetryPolicyBuilder WithCondition(RetryConditionHandler condition)
        {
            conditions.Add(condition);

            return this;
        }

        /// <summary>
        /// Appends max retry count limit condition to the current instance of the builder
        /// </summary>
        /// <param name="maxRetries">Max retry attempts count</param>
        /// <returns>Current instance of the builder</returns>
        public RetryPolicyBuilder WithMaxRetries(int maxRetries)
        {
            return WithCondition((int attempt, TimeSpan time, ref TimeSpan delay) => attempt <= maxRetries);
        }

        /// <summary>
        /// Appends handler which sets retry delay to a fixed amount of time
        /// </summary>
        /// <param name="retryDelay">Retry delay</param>
        /// <returns>Current instance of the builder</returns>
        public RetryPolicyBuilder WithDelay(TimeSpan retryDelay)
        {
            return WithCondition((int attempt, TimeSpan time, ref TimeSpan delay) =>
            {
                delay = retryDelay;
                return true;
            });
        }

        /// <summary>
        /// Appends handler which sets retry delay to a fixed amount of time
        /// </summary>
        /// <param name="retryDelayMilliseconds">Retry delay in milliseconds</param>
        /// <returns>Current instance of the builder</returns>
        public RetryPolicyBuilder WithDelay(int retryDelayMilliseconds)
        {
            return WithDelay(FromMilliseconds(retryDelayMilliseconds));
        }

        /// <summary>
        /// Appends handler which sets delay according to exponential function where retry attempt is exponent
        /// </summary>
        /// <param name="baseMilliseconds">Base value of exponential function in milliseconds</param>
        /// <returns>Current instance of the builder</returns>
        public RetryPolicyBuilder WithExponentialDelay(double baseMilliseconds = 2000)
        {
            return WithCondition((int attempt, TimeSpan time, ref TimeSpan delay) =>
            {
                delay = FromMilliseconds(Math.Pow(baseMilliseconds, attempt));
                return true;
            });
        }

        /// <summary>
        /// Appends handler to add random jitter time to the current delay value
        /// </summary>
        /// <param name="minMilliseconds">Minimal amount of milliseconds to add</param>
        /// <param name="maxMilliseconds">Maximum amount of milliseconds to add</param>
        /// <returns>Current instance of the builder</returns>
        public RetryPolicyBuilder WithJitter(int minMilliseconds = 500, int maxMilliseconds = 10000)
        {
            var random = new Random();

            return WithCondition((int attempt, TimeSpan time, ref TimeSpan delay) =>
            {
                delay = delay.Add(FromMilliseconds(random.Next(minMilliseconds, maxMilliseconds)));
                return true;
            });
        }
    }
}