namespace OOs.Memory;

public static class InterlockedExtensions
{
    public static int CompareDecrement(ref int location, int minComparand)
    {
        var sw = new SpinWait();
        while (true)
        {
            var current = Volatile.Read(ref location);
            var value = current - 1;
            if (value < minComparand || Interlocked.CompareExchange(ref location, value, current) == current)
                return current;

            sw.SpinOnce();
        }
    }
}