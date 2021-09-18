using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace System.Policies;

public readonly struct RepeatPolicyBuilder : IEquatable<RepeatPolicyBuilder>
{
    private ImmutableList<RepeatCondition> Conditions { get; }

    private RepeatPolicyBuilder(ImmutableList<RepeatCondition> conditions)
    {
        Conditions = conditions;
    }

    /// <summary>
    /// Creates new instance of the repeat policy
    /// </summary>
    /// <returns>New instance of the policy</returns>
    public IRepeatPolicy Build()
    {
        return new ConditionalRepeatPolicy((Conditions ?? ImmutableList<RepeatCondition>.Empty).ToArray());
    }

    /// <summary>
    /// Appends custom repeat condition handler to the current instance of the builder
    /// </summary>
    /// <param name="condition">Condition to add</param>
    /// <returns>New instance of the builder</returns>
    public RepeatPolicyBuilder WithCondition(RepeatCondition condition)
    {
        return new((Conditions ?? ImmutableList<RepeatCondition>.Empty).Add(condition));
    }

    /// <summary>
    /// Appends repeat threshold condition to the current instance of the builder
    /// </summary>
    /// <param name="maxRetries">Max repeat attempts count</param>
    /// <returns>New instance of the builder</returns>
    public RepeatPolicyBuilder WithThreshold(int maxRetries)
    {
        return WithCondition((Exception _, int attempt, TimeSpan _, ref TimeSpan _) => attempt <= maxRetries);
    }

        /// <summary>
        /// Appends handler which sets repeat delay to a fixed amount of time
        /// </summary>
        /// <param name="interval">repeat delay</param>
        /// <returns>New instance of the builder</returns>
        public RepeatPolicyBuilder WithInterval(TimeSpan interval)
        {
            return WithCondition((Exception _, int _, TimeSpan _, ref TimeSpan delay) =>
            {
                delay = interval;
                return true;
            });
        }

        /// <summary>
        /// Appends handler which sets delay according to exponential function where repeat attempt is exponent
        /// </summary>
        /// <param name="baseSeconds">Base value of exponential function in seconds</param>
        /// <param name="limitSeconds">Top limit value in seconds</param>
        /// <returns>New instance of the builder</returns>
        public RepeatPolicyBuilder WithExponentialInterval(double baseSeconds, double limitSeconds)
        {
            if(baseSeconds <= 1) throw new ArgumentException("Value must be greater then 1.0", nameof(baseSeconds));
            return WithCondition((Exception _, int attempt, TimeSpan _, ref TimeSpan delay) =>
            {
                delay = TimeSpan.FromSeconds(Math.Min(Math.Pow(baseSeconds, attempt), limitSeconds));
                return true;
            });
        }

    /// <summary>
    /// Appends handler to add random jitter time to the current interval value
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
    /// Appends handler that checks whether operation exception should break repeat loop
    /// </summary>
    /// <typeparam name="T">Exception type</typeparam>
    /// <returns>New instance of the builder</returns>
    public RepeatPolicyBuilder WithBreakingException<T>() where T : Exception
    {
        return WithCondition((Exception exception, int _, TimeSpan _, ref TimeSpan _) => exception is not T);
    }

    public override bool Equals(object obj)
    {
        return obj is RepeatPolicyBuilder { Conditions: { } c } && c == Conditions;
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
        return other.Conditions == Conditions;
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