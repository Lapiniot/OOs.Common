namespace OOs.Memory;

internal static class InterlockedExtensions
{
    extension(Interlocked)
    {
        public static int CompareDecrement(ref int location, int minComparand)
        {
            SpinWait sw = default;
            while (true)
            {
                var current = Volatile.Read(ref location);
                var value = current - 1;
                if (value < minComparand || Interlocked.CompareExchange(ref location, value, current) == current)
                {
                    return current;
                }

                sw.SpinOnce(sleep1Threshold: -1);
            }
        }
    }
}