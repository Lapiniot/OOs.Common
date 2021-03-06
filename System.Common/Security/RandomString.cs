using System.Text;

namespace System.Security
{
    public static class RandomString
    {
        public const string AlphaNumeric = "abcdefghijklmnopqrstuvwxyz0123456789";
        public const string AlphaNumericCapitalCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public static string Generate(int length, byte minCharCode, byte maxCharCode)
        {
            Span<byte> bytes = length <= 256 ? stackalloc byte[length] : new byte[length];
            var rnd = new Random();
            for(int i = 0; i < bytes.Length; i++) bytes[i] = (byte)rnd.Next(minCharCode, maxCharCode + 1);

            return Encoding.ASCII.GetString(bytes);
        }

        public static string Generate(int length, string alphabet)
        {
            Span<byte> bytes = length <= 256 ? stackalloc byte[length] : new byte[length];
            var rnd = new Random();
            for(int i = 0; i < bytes.Length; i++) bytes[i] = (byte)alphabet[rnd.Next(0, alphabet.Length)];

            return Encoding.ASCII.GetString(bytes);
        }
    }
}