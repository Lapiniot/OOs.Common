using System.Runtime.CompilerServices;
using System.Threading;

namespace System
{
    public static class CorrelationIdGenerator
    {
        private static long current = DateTimeOffset.UtcNow.Ticks;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetNext()
        {
            return Interlocked.Increment(ref current);
        }
    }
}