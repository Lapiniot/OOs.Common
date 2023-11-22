using System.Security.Cryptography;

namespace System.Net.Http.WebPush;

public static class ServerKeyGenerator
{
    public static (byte[] PublicKey, byte[] PrivateKey) Generate()
    {
        using var ecdh = ECDiffieHellman.Create();
        ecdh.GenerateKey(ECCurve.NamedCurves.nistP256);
        var parameters = ecdh.ExportParameters(true);
        return (CryptoHelpers.GetBytes(parameters.Q), parameters.D);
    }
}