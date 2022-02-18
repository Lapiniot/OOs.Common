namespace System.Net.Http;

public static class Encoders
{
    public static string ToBase64String(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    public static byte[] FromBase64String(string base64String)
    {
        ArgumentNullException.ThrowIfNull(base64String);

        var len = base64String.Length;
        var totalWidth = len % 4 == 0 ? len : ((len >> 2) + 1) << 2;
        return Convert.FromBase64String(base64String.Replace('-', '+').Replace('_', '/').PadRight(totalWidth, '='));
    }
}