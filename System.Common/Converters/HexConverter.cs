using static System.Globalization.NumberStyles;

namespace System.Converters
{
    public static class HexConverter
    {
        private const string ErrorMessage = "Invalid character in hex string";

        public static string ToHexString(this byte[] bytes)
        {
            var buffer = new char[bytes.Length * 2];

            for(var i = 0; i < bytes.Length; i++)
            {
                var b = bytes[i];
                var high = b >> 4;
                var low = b & 0x0f;
                buffer[i << 1] = (char)(high < 0x0a ? 0x30 + high : 0x57 + high);
                buffer[(i << 1) + 1] = (char)(low < 0x0a ? 0x30 + low : 0x57 + low);
            }

            return new string(buffer);
        }

        public static byte[] FromHexString(string hexString)
        {
            var bytes = new byte[hexString.Length >> 1];

            for(var i = 0; i < bytes.Length; i++)
            {
                var h = hexString[i * 2];
                var l = hexString[i * 2 + 1];

                var high = h >= '0' && h <= '9' ? h - '0' :
                    h >= 'a' && h <= 'f' ? h - 'W' :
                    h >= 'A' && h <= 'F' ? h - '7' : throw new ArgumentException(ErrorMessage);
                var low = l >= '0' && l <= '9' ? l - '0' :
                    l >= 'a' && l <= 'f' ? l - 'W' :
                    h >= 'A' && h <= 'F' ? h - '7' : throw new ArgumentException(ErrorMessage);

                bytes[i] = (byte)((high << 4) | low);
            }

            return bytes;
        }

        public static bool TryParse(string s, out uint result)
        {
            return s.StartsWith("0x") && uint.TryParse(s.Substring(2), HexNumber, null, out result) ||
                   uint.TryParse(s, Integer & ~AllowLeadingSign, null, out result);
        }

        public static bool TryParse(string s, out int result)
        {
            return s.StartsWith("0x") && int.TryParse(s.Substring(2), HexNumber, null, out result) ||
                   int.TryParse(s, Integer & ~AllowLeadingSign, null, out result);
        }

        public static bool TryParse(string s, out long result)
        {
            return s.StartsWith("0x") && long.TryParse(s.Substring(2), HexNumber, null, out result) ||
                   long.TryParse(s, Integer & ~AllowLeadingSign, null, out result);
        }

        public static bool TryParse(string s, out ulong result)
        {
            return s.StartsWith("0x") && ulong.TryParse(s.Substring(2), HexNumber, null, out result) ||
                   ulong.TryParse(s, Integer & ~AllowLeadingSign, null, out result);
        }
    }
}