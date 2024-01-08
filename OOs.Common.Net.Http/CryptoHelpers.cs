using System.Security.Cryptography;

namespace OOs.Net.Http;

internal static class CryptoHelpers
{
    public static byte[] GenerateSalt(int size)
    {
        var salt = new byte[size];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    public static ECDiffieHellmanPublicKey ImportP256DHPublicKey(byte[] publicKey)
    {
        using var ecdh = ECDiffieHellman.Create(ImportECParameters(publicKey, null));
        return ecdh.PublicKey;
    }

    public static ECParameters ImportECParameters(byte[] publicKey, byte[] privateKey) =>
        new()
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = publicKey.AsSpan(1, 32).ToArray(), Y = publicKey.AsSpan(33, 32).ToArray() },
            D = privateKey
        };

    public static byte[] GetBytes(ECPoint point)
    {
        var buffer = new byte[65];
        buffer[0] = 0x04;
        point.X.CopyTo(buffer.AsSpan(1));
        point.Y.CopyTo(buffer.AsSpan(33));
        return buffer;
    }

    public static byte[] DeriveKeyFromHmac(ECDiffieHellman ecdh, byte[] otherPartyPublicKey, byte[] hmacKey)
    {
        ArgumentNullException.ThrowIfNull(ecdh);
        ArgumentNullException.ThrowIfNull(otherPartyPublicKey);
        ArgumentNullException.ThrowIfNull(hmacKey);

        using var publicKey = ImportP256DHPublicKey(otherPartyPublicKey);
        return ecdh.DeriveKeyFromHmac(publicKey, HashAlgorithmName.SHA256, hmacKey);
    }

    public static byte[] ComputeHKDF(byte[] salt, byte[] ikm, byte[] data, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        using var hmac = new HMACSHA256(salt);
        var key = hmac.ComputeHash(ikm);
        var buffer = new byte[data.Length + 1];
        data.CopyTo(buffer, 0);
        buffer[^1] = 0x01;
        hmac.Key = key;
        hmac.Initialize();
        return hmac.ComputeHash(buffer).AsSpan(0, length).ToArray();
    }
}