using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System
{
    public abstract class IdentityPool<T> : IIdentityPool<T>
    {
        private readonly HashSet<T> hashSet = new HashSet<T>();
        private SpinLock spinLock = new SpinLock();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract void Advance(ref T value);

        #region Implementation of IIdentityPool<ushort>

        public T Rent()
        {
            T i = default;
            var lockTaken = false;
            try
            {
                spinLock.Enter(ref lockTaken);

                while(hashSet.Contains(i))
                {
                    Advance(ref i);
                }

                hashSet.Add(i);
            }
            finally
            {
                if(lockTaken) spinLock.Exit(false);
            }

            return i;
        }

        public void Return(in T identity)
        {
            var lockTaken = false;
            try
            {
                spinLock.Enter(ref lockTaken);
                hashSet.Remove(identity);
            }
            finally
            {
                if(lockTaken) spinLock.Exit(false);
            }
        }

        #endregion
    }

    public class UInt16IdentityPool : IdentityPool<ushort>
    {
        #region Overrides of IdentityPool<ushort>

        protected override void Advance(ref ushort value)
        {
            value++;
        }

        #endregion
    }
}