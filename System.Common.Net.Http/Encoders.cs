namespace System.Net.Http
{
    public static class Encoders
    {
        public static string ToBase64String(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        public static byte[] FromBase64String(string base64String)
        {
            if(base64String is null) throw new ArgumentNullException(nameof(base64String));

            int len = base64String.Length;
            int totalWidth = len % 4 == 0 ? len : ((len >> 2) + 1) << 2;
            return Convert.FromBase64String(base64String.Replace('-', '+').Replace('_', '/').PadRight(totalWidth, '='));
        }
    }
}