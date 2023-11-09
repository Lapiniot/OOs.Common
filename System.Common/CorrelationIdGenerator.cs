namespace System;

public static class CorrelationIdGenerator
{
    private static long current = DateTimeOffset.UtcNow.Ticks;

    public static long GetNext() => Interlocked.Increment(ref current);
}