using System.Runtime.CompilerServices;

namespace System
{
    public static class Base32
    {
        private static readonly char[] alphabet =
        {
            '0', '1', '2', '3', '4', '5', '6', '7',
            '8', '9', 'A', 'B', 'C', 'D', 'E', 'F',
            'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N',
            'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V'
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToBase32String(long value)
        {
            return string.Create(13, value, WriteBuffer);
        }

        private static void WriteBuffer(Span<char> span, long value)
        {
            span[12] = alphabet[(int)(value & 31)];
            span[11] = alphabet[(int)((value >> 5) & 31)];
            span[10] = alphabet[(int)((value >> 10) & 31)];
            span[9] = alphabet[(int)((value >> 15) & 31)];
            span[8] = alphabet[(int)((value >> 20) & 31)];
            span[7] = alphabet[(int)((value >> 25) & 31)];
            span[6] = alphabet[(int)((value >> 30) & 31)];
            span[5] = alphabet[(int)((value >> 35) & 31)];
            span[4] = alphabet[(int)((value >> 40) & 31)];
            span[3] = alphabet[(int)((value >> 45) & 31)];
            span[2] = alphabet[(int)((value >> 50) & 31)];
            span[1] = alphabet[(int)((value >> 55) & 31)];
            span[0] = alphabet[(int)((value >> 60) & 31)];
        }
    }
}