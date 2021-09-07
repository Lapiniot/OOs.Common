namespace System.Threading;

public static class ReaderWriterLockSlimExtensions
{
    public static ReadLockScope WithReadLock(this ReaderWriterLockSlim lockSlim)
    {
        ArgumentNullException.ThrowIfNull(lockSlim);

        return new ReadLockScope(lockSlim);
    }

    public static UpgradeableReadLockScope WithUpgradeableReadLock(this ReaderWriterLockSlim lockSlim)
    {
        ArgumentNullException.ThrowIfNull(lockSlim);

        return new UpgradeableReadLockScope(lockSlim);
    }

    public static WriteLockScope WithWriteLock(this ReaderWriterLockSlim lockSlim)
    {
        ArgumentNullException.ThrowIfNull(lockSlim);

        return new WriteLockScope(lockSlim);
    }
}

public readonly struct ReadLockScope : IEquatable<ReadLockScope>, IDisposable
{
    private readonly ReaderWriterLockSlim lockSlim;

    internal ReadLockScope(ReaderWriterLockSlim lockSlim)
    {
        this.lockSlim = lockSlim;
        lockSlim.EnterReadLock();
    }

    public void Dispose()
    {
        lockSlim.ExitReadLock();
    }

    public override bool Equals(object obj)
    {
        return obj is ReadLockScope scope && lockSlim == scope.lockSlim;
    }

    public bool Equals(ReadLockScope other)
    {
        return other.lockSlim == lockSlim;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(lockSlim);
    }

    public static bool operator ==(ReadLockScope scope1, ReadLockScope scope2)
    {
        return scope1.lockSlim == scope2.lockSlim;
    }

    public static bool operator !=(ReadLockScope scope1, ReadLockScope scope2)
    {
        return scope1.lockSlim == scope2.lockSlim;
    }
}

public readonly struct UpgradeableReadLockScope : IEquatable<UpgradeableReadLockScope>, IDisposable
{
    private readonly ReaderWriterLockSlim lockSlim;

    internal UpgradeableReadLockScope(ReaderWriterLockSlim lockSlim)
    {
        this.lockSlim = lockSlim;
        lockSlim.EnterUpgradeableReadLock();
    }

    public void Dispose()
    {
        lockSlim.ExitUpgradeableReadLock();
    }

    public override bool Equals(object obj)
    {
        return obj is UpgradeableReadLockScope scope && lockSlim == scope.lockSlim;
    }

    public bool Equals(UpgradeableReadLockScope other)
    {
        return other.lockSlim == lockSlim;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(lockSlim);
    }

    public static bool operator ==(UpgradeableReadLockScope scope1, UpgradeableReadLockScope scope2)
    {
        return scope1.lockSlim == scope2.lockSlim;
    }

    public static bool operator !=(UpgradeableReadLockScope scope1, UpgradeableReadLockScope scope2)
    {
        return scope1.lockSlim == scope2.lockSlim;
    }
}

public readonly struct WriteLockScope : IEquatable<WriteLockScope>, IDisposable
{
    private readonly ReaderWriterLockSlim lockSlim;

    internal WriteLockScope(ReaderWriterLockSlim lockSlim)
    {
        this.lockSlim = lockSlim;
        lockSlim.EnterWriteLock();
    }

    public void Dispose()
    {
        lockSlim.ExitWriteLock();
    }

    public override bool Equals(object obj)
    {
        return obj is WriteLockScope scope && lockSlim == scope.lockSlim;
    }

    public bool Equals(WriteLockScope other)
    {
        return other.lockSlim == lockSlim;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(lockSlim);
    }

    public static bool operator ==(WriteLockScope scope1, WriteLockScope scope2)
    {
        return scope1.lockSlim == scope2.lockSlim;
    }

    public static bool operator !=(WriteLockScope scope1, WriteLockScope scope2)
    {
        return scope1.lockSlim == scope2.lockSlim;
    }
}