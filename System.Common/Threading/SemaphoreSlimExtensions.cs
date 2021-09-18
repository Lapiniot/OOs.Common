namespace System.Threading;

public static class SemaphoreSlimExtensions
{
    public static async Task<SemaphoreSlimLockScope> GetLockAsync(this SemaphoreSlim semaphoreSlim, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(semaphoreSlim);

        await semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

        return new SemaphoreSlimLockScope(semaphoreSlim);
    }
}

public readonly struct SemaphoreSlimLockScope : IEquatable<SemaphoreSlimLockScope>, IDisposable
{
    private readonly SemaphoreSlim semaphoreSlim;

    internal SemaphoreSlimLockScope(SemaphoreSlim semaphoreSlim)
    {
        ArgumentNullException.ThrowIfNull(semaphoreSlim);
        this.semaphoreSlim = semaphoreSlim;
    }

    #region Implementation of IDisposable

    public void Dispose()
    {
        _ = semaphoreSlim.Release();
    }

    #endregion

    public static bool operator ==(SemaphoreSlimLockScope scope1, SemaphoreSlimLockScope scope2)
    {
        return scope1.semaphoreSlim == scope2.semaphoreSlim;
    }

    public static bool operator !=(SemaphoreSlimLockScope scope1, SemaphoreSlimLockScope scope2)
    {
        return scope1.semaphoreSlim == scope2.semaphoreSlim;
    }

    public override bool Equals(object obj)
    {
        return obj is SemaphoreSlimLockScope scope && semaphoreSlim == scope.semaphoreSlim;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(semaphoreSlim);
    }

    public bool Equals(SemaphoreSlimLockScope other)
    {
        return semaphoreSlim == other.semaphoreSlim;
    }
}