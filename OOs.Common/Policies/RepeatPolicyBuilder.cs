using System.Collections.Immutable;

namespace OOs.Policies;

#pragma warning disable IDE0350 // Use implicitly typed lambda
public readonly record struct RepeatPolicyBuilder(ImmutableList<RepeatCondition> Conditions)
{
    /// <summary>
    /// Creates new instance of the repeat policy
    /// </summary>
    /// <returns>New instance of the policy</returns>
    public IRepeatPolicy Build() => new ConditionalRepeatPolicy([.. Conditions ?? []]);

    /// <summary>
    /// Appends custom repeat condition handler to the current instance of the builder
    /// </summary>
    /// <param name="condition">Condition to add</param>
    /// <returns>New instance of the builder</returns>
    public RepeatPolicyBuilder WithCondition(RepeatCondition condition) =>
        new((Conditions ?? []).Add(condition));

    /// <summary>
    /// Appends repeat threshold condition to the current instance of the builder
    /// </summary>
    /// <param name="maxRetries">Max repeat attempts count</param>
    /// <returns>New instance of the builder</returns>
    public RepeatPolicyBuilder WithThreshold(int maxRetries) =>
        WithCondition((Exception _, int attempt, TimeSpan _, ref TimeSpan _)
            => attempt <= maxRetries);

    /// <summary>
    /// Appends handler which sets repeat delay to a fixed amount of time
    /// </summary>
    /// <param name="interval">repeat delay</param>
    /// <returns>New instance of the builder</returns>
    public RepeatPolicyBuilder WithInterval(TimeSpan interval) =>
        WithCondition((Exception _, int _, TimeSpan _, ref TimeSpan delay) =>
        {
            delay = interval;
            return true;
        });

    /// <summary>
    /// Appends handler which sets delay according to exponential function where repeat attempt is exponent
    /// </summary>
    /// <param name="baseSeconds">Base value of exponential function in seconds</param>
    /// <param name="limitSeconds">Top limit value in seconds</param>
    /// <returns>New instance of the builder</returns>
    public RepeatPolicyBuilder WithExponentialInterval(double baseSeconds, double limitSeconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(baseSeconds, 1.0);
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
    public RepeatPolicyBuilder WithJitter(int minMilliseconds = 500, int maxMilliseconds = 10000) =>
        WithCondition((Exception _, int _, TimeSpan _, ref TimeSpan delay) =>
        {
#pragma warning disable CA5394 // Do not use insecure randomness
            delay = delay.Add(TimeSpan.FromMilliseconds(Random.Shared.Next(minMilliseconds, maxMilliseconds)));
#pragma warning restore CA5394 // Do not use insecure randomness
            return true;
        });

    /// <summary>
    /// Appends handler that checks whether operation exception should break repeat loop
    /// </summary>
    /// <typeparam name="T">Exception type</typeparam>
    /// <returns>New instance of the builder</returns>
    public RepeatPolicyBuilder WithBreakingException<T>() where T : Exception =>
        WithCondition((Exception exception, int _, TimeSpan _, ref TimeSpan _) => exception is not T);
}