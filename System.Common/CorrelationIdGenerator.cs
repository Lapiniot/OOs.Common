using System.Runtime.CompilerServices;

namespace System;

public static class CorrelationIdGenerator
{
    private static long current = DateTimeOffset.UtcNow.Ticks;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetNext() => Interlocked.Increment(ref current);
}