namespace System.Threading
{
    public static class ReaderWriterLockSlimExtensions
    {
        public static ReadLockScope WithReadLock(this ReaderWriterLockSlim lockSlim)
        {
            return new ReadLockScope(lockSlim);
        }

        public static UpgradeableReadLockScope WithUpgradeableReadLock(this ReaderWriterLockSlim lockSlim)
        {
            return new UpgradeableReadLockScope(lockSlim);
        }

        public static WriteLockScope WithWriteLock(this ReaderWriterLockSlim lockSlim)
        {
            return new WriteLockScope(lockSlim);
        }
    }

    public readonly struct ReadLockScope : IDisposable
    {
        private readonly ReaderWriterLockSlim lockSlim;

        public ReadLockScope(ReaderWriterLockSlim lockSlim)
        {
            this.lockSlim = lockSlim;
            lockSlim.EnterReadLock();
        }

        public void Dispose()
        {
            lockSlim.ExitReadLock();
        }
    }

    public readonly struct UpgradeableReadLockScope : IDisposable
    {
        private readonly ReaderWriterLockSlim lockSlim;

        public UpgradeableReadLockScope(ReaderWriterLockSlim lockSlim)
        {
            this.lockSlim = lockSlim;
            lockSlim.EnterUpgradeableReadLock();
        }

        public void Dispose()
        {
            lockSlim.ExitUpgradeableReadLock();
        }
    }

    public readonly struct WriteLockScope : IDisposable
    {
        private readonly ReaderWriterLockSlim lockSlim;

        public WriteLockScope(ReaderWriterLockSlim lockSlim)
        {
            this.lockSlim = lockSlim;
            lockSlim.EnterWriteLock();
        }

        public void Dispose()
        {
            lockSlim.ExitWriteLock();
        }
    }
}