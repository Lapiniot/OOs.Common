namespace OOs.Policies;

public interface IRepeatPolicy
{
    Task RepeatAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
}