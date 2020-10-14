﻿using System.Collections.Immutable;
using System.Linq;

namespace System.Policies
{
    public readonly struct RepeatPolicyBuilder : IEquatable<RepeatPolicyBuilder>
    {
        private ImmutableList<RepeatCondition> conditions { get; }

        private RepeatPolicyBuilder(ImmutableList<RepeatCondition> conditions)
        {
            this.conditions = conditions;
        }

        /// <summary>
        /// Creates new instance of the retry policy
        /// </summary>
        /// <returns>New instance of the policy</returns>
        public IRepeatPolicy Build()
        {
            return new ConditionalRepeatPolicy((conditions ?? ImmutableList<RepeatCondition>.Empty).ToArray());
        }

        /// <summary>
        /// Appends custom retry condition handler to the current instance of the builder
        /// </summary>
        /// <param name="condition">Condition to add</param>
        /// <returns>New instance of the builder</returns>
        public RepeatPolicyBuilder WithCondition(RepeatCondition condition)
        {
            return new RepeatPolicyBuilder((conditions ?? ImmutableList<RepeatCondition>.Empty).Add(condition));
        }

        /// <summary>
        /// Appends retry threshold condition to the current instance of the builder
        /// </summary>
        /// <param name="maxRetries">Max retry attempts count</param>
        /// <returns>New instance of the builder</returns>
        public RepeatPolicyBuilder WithThreshold(int maxRetries)
        {
            return WithCondition((Exception _, int attempt, TimeSpan _, ref TimeSpan delay) => attempt <= maxRetries);
        }

        /// <summary>
        /// Appends handler which sets retry delay to a fixed amount of time
        /// </summary>
        /// <param name="retryDelay">Retry delay</param>
        /// <returns>New instance of the builder</returns>
        public RepeatPolicyBuilder WithDelay(TimeSpan retryDelay)
        {
            return WithCondition((Exception _, int _, TimeSpan _, ref TimeSpan delay) =>
            {
                delay = retryDelay;
                return true;
            });
        }

        /// <summary>
        /// Appends handler which sets delay according to exponential function where retry attempt is exponent
        /// </summary>
        /// <param name="baseSeconds">Base value of exponential function in milliseconds</param>
        /// <param name="baseSeconds">Top limit value in milliseconds</param>
        /// <returns>New instance of the builder</returns>
        public RepeatPolicyBuilder WithExponentialDelay(double baseSeconds, double limitSeconds)
        {
            if(baseSeconds <= 1) throw new ArgumentException("Value must be greater then 1.0", nameof(baseSeconds));
            return WithCondition((Exception _, int attempt, TimeSpan _, ref TimeSpan delay) =>
            {
                delay = TimeSpan.FromSeconds(Math.Min(Math.Pow(baseSeconds, attempt), limitSeconds));
                return true;
            });
        }

        /// <summary>
        /// Appends handler to add random jitter time to the current delay value
        /// </summary>
        /// <param name="minMilliseconds">Minimal amount of milliseconds to add</param>
        /// <param name="maxMilliseconds">Maximum amount of milliseconds to add</param>
        /// <returns>New instance of the builder</returns>
        public RepeatPolicyBuilder WithJitter(int minMilliseconds = 500, int maxMilliseconds = 10000)
        {
            return WithCondition((Exception _, int _, TimeSpan _, ref TimeSpan delay) =>
            {
                delay = delay.Add(TimeSpan.FromMilliseconds(new Random().Next(minMilliseconds, maxMilliseconds)));
                return true;
            });
        }

        /// <summary>
        /// Appends handler that checks whether operation exception should breake retry loop
        /// </summary>
        /// <typeparam name="T">Exception type</typeparam>
        /// <returns>New instance of the builder</returns>
        public RepeatPolicyBuilder WithBreakingException<T>() where T : Exception
        {
            return WithCondition((Exception exception, int _, TimeSpan _, ref TimeSpan _) => exception is not T);
        }

        public override bool Equals(object obj)
        {
            return obj is RepeatPolicyBuilder { conditions: { } c } && c == conditions;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return nameof(RepeatPolicyBuilder);
        }

        public bool Equals(RepeatPolicyBuilder other)
        {
            return other.conditions == conditions;
        }

        public static bool operator ==(RepeatPolicyBuilder b1, RepeatPolicyBuilder b2)
        {
            return b1.Equals(b2);
        }

        public static bool operator !=(RepeatPolicyBuilder b1, RepeatPolicyBuilder b2)
        {
            return !b1.Equals(b2);
        }
    }
}