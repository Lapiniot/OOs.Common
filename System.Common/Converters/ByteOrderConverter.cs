namespace System.Converters
{
    public static class ByteOrderConverter
    {
        public static short ChangeByteOrder(this short value)
        {
            return (short)((value << 8) | (value >> 8));
        }

        public static ushort ChangeByteOrder(this ushort value)
        {
            return (ushort)((value << 8) | (value >> 8));
        }

        public static int ChangeByteOrder(this int value)
        {
            return (value << 24) | ((value << 8) & 0x00ff0000) | ((value >> 8) & 0x0000ff00) | (value >> 24);
        }

        public static uint ChangeByteOrder(this uint value)
        {
            return (value << 24) | ((value << 8) & 0x00ff0000) | ((value >> 8) & 0x0000ff00) | (value >> 24);
        }

        public static long ChangeByteOrder(this long value)
        {
            return (value << 56) | ((value << 40) & 0x00ff0000_00000000) |
                   ((value << 24) & 0x0000ff00_00000000) | ((value << 8) & 0x000000ff_00000000) |
                   (value >> 56) | ((value >> 40) & 0x00000000_0000ff00) |
                   ((value >> 24) & 0x00000000_00ff0000) | ((value >> 8) & 0x00000000_ff000000);
        }

        public static ulong ChangeByteOrder(this ulong value)
        {
            return (value << 56) | ((value << 40) & 0x00ff0000_00000000) |
                   ((value << 24) & 0x0000ff00_00000000) | ((value << 8) & 0x000000ff_00000000) |
                   (value >> 56) | ((value >> 40) & 0x00000000_0000ff00) |
                   ((value >> 24) & 0x00000000_00ff0000) | ((value >> 8) & 0x00000000_ff000000);
        }

        public static char ChangeByteOrder(this char value)
        {
            return (char)((value << 8) | (value >> 8));
        }

        public static double ChangeByteOrder(this double value)
        {
            return BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(value).ChangeByteOrder());
        }

        public static decimal ChangeByteOrder(this decimal readDecimal)
        {
            void SwapAndReverse(int[] array, int index1, int index2)
            {
                var temp = array[index1].ChangeByteOrder();

                array[index1] = array[index2].ChangeByteOrder();

                array[index2] = temp;
            }

            var words = decimal.GetBits(readDecimal);

            SwapAndReverse(words, 0, 3);

            SwapAndReverse(words, 1, 2);

            return new decimal(words);
        }
    }
}