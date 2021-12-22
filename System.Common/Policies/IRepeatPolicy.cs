namespace System.Policies;

public interface IRepeatPolicy
{
    Task RepeatAsync(Func<CancellationToken, ValueTask> operation, CancellationToken cancellationToken = default);
}