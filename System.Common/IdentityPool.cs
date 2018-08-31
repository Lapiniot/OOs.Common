using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System
{
    public abstract class IdentityPool<T> : IIdentityPool<T>
    {
        private readonly HashSet<T> hashSet = new HashSet<T>();
        private SpinLock rentLock = new SpinLock();
        private SpinLock updateLock = new SpinLock();

        protected abstract void Advance(ref T value);

        protected abstract T GetStartValue();

        #region Implementation of IIdentityPool<ushort>

        public T Rent()
        {
            var i = GetStartValue();
            var lockTaken = false;
            try
            {
                rentLock.Enter(ref lockTaken);

                while(hashSet.Contains(i))
                {
                    Advance(ref i);
                }

                var updateLockTaken = false;
                try
                {
                    updateLock.Enter(ref updateLockTaken);
                    hashSet.Add(i);
                }
                finally
                {
                    if(updateLockTaken) updateLock.Exit(false);
                }
            }
            finally
            {
                if(lockTaken) rentLock.Exit(false);
            }

            return i;
        }

        public void Return(in T identity)
        {
            var lockTaken = false;
            try
            {
                updateLock.Enter(ref lockTaken);
                hashSet.Remove(identity);
            }
            finally
            {
                if(lockTaken) updateLock.Exit(false);
            }
        }

        #endregion
    }

    public class UInt16IdentityPool : IdentityPool<ushort>
    {
        private readonly ushort endValue;
        private readonly ushort startValue;

        #region Overrides of IdentityPool<ushort>

        public UInt16IdentityPool(ushort startValue = ushort.MinValue, ushort endValue = ushort.MaxValue)
        {
            this.startValue = startValue;
            this.endValue = endValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void Advance(ref ushort value)
        {
            if(value == endValue)
            {
                value = startValue;
            }
            else
            {
                value++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override ushort GetStartValue()
        {
            return startValue;
        }

        #endregion
    }
}