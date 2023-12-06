using System.Buffers.Text;

namespace System.Net.Http;

public static class Base64UrlSafe
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

    public static void EncodeToUtf8InPlace(Span<byte> buffer, int dataLength, out int bytesWritten)
    {
        Base64.EncodeToUtf8InPlace(buffer, dataLength, out bytesWritten);

        var index = bytesWritten - 1;

        while (index >= 0 && buffer[index] == '=') index--;
        bytesWritten = index + 1;

        for (; index >= 0; index--)
        {
            ref var b = ref buffer[index];
            if (b == '+')
                b = (byte)'-';
            else if (b == '/')
                b = (byte)'_';
        }
    }
}