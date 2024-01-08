using System.Buffers.Text;

namespace OOs.Common.Benchmarks.Base64UrlSafe;

public static class Base64UrlSafeV1
{
    public static string ToBase64String(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    public static byte[] FromBase64String(string base64String)
    {
        ArgumentNullException.ThrowIfNull(base64String);

        var len = base64String.Length;
        var totalWidth = len % 4 == 0 ? len : (len >> 2) + 1 << 2;
        return Convert.FromBase64String(base64String.Replace('-', '+').Replace('_', '/').PadRight(totalWidth, '='));
    }

    public static void EncodeToUtf8InPlace(Span<byte> buffer, int dataLength, out int bytesWritten)
    {
        Base64.EncodeToUtf8InPlace(buffer, dataLength, out bytesWritten);
        ConvertToUrlSafe(buffer.Slice(0, bytesWritten), out bytesWritten);
    }

    public static void EncodeToUtf8(Span<byte> bytes, Span<byte> utf8, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true)
    {
        Base64.EncodeToUtf8(bytes, utf8, out bytesConsumed, out bytesWritten, isFinalBlock);
        ConvertToUrlSafe(utf8.Slice(0, bytesWritten), out bytesWritten);
    }

    private static void ConvertToUrlSafe(Span<byte> utf8, out int bytesWritten)
    {
        var index = utf8.Length - 1;
        while (index >= 0 && utf8[index] == '=') index--;
        bytesWritten = index + 1;

        for (; index >= 0; index--)
        {
            ref var b = ref utf8[index];
            if (b == '+')
                b = (byte)'-';
            else if (b == '/')
                b = (byte)'_';
        }
    }
}