using System.Security.Cryptography;
using System.Text;

namespace OOs.Security;

public static class RandomString
{
    public const string AlphaNumeric = "abcdefghijklmnopqrstuvwxyz0123456789";
    public const string AlphaNumericCapitalCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public static string Generate(int length, byte minCharCode, byte maxCharCode)
    {
        var bytes = length <= 256 ? stackalloc byte[length] : new byte[length];

        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)RandomNumberGenerator.GetInt32(minCharCode, maxCharCode + 1);
        }

        return Encoding.ASCII.GetString(bytes);
    }

    public static string Generate(int length, string alphabet)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alphabet);

        var bytes = length <= 256 ? stackalloc byte[length] : new byte[length];

        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)alphabet[RandomNumberGenerator.GetInt32(0, alphabet.Length)];
        }

        return Encoding.ASCII.GetString(bytes);
    }
}