using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace System.Net.Http
{
    public static class CryptoExtensions
    {
        public static byte[] GenerateSalt(int size)
        {
            var salt = new byte[size];
            RandomNumberGenerator.Fill(salt);
            return salt;
        }

        public static ECDiffieHellmanPublicKey ImportP256DHPublicKey(byte[] publicKey)
        {
            using(var ecdh = ECDiffieHellman.Create(ImportECParameters(publicKey, null)))
            {
                return ecdh.PublicKey;
            }
        }

        public static byte[] ExportP256DHPublicKey(ECDiffieHellmanPublicKey publicKey)
        {
            if(publicKey is null) throw new ArgumentNullException(nameof(publicKey));

            var parameters = publicKey.ExportParameters();
            return GetBytes(parameters.Q);
        }

        public static byte[] ExportP256DHPublicKey(this ECDiffieHellman ecdh)
        {
            if(ecdh is null) throw new ArgumentNullException(nameof(ecdh));

            return ExportP256DHPublicKey(ecdh.PublicKey);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ECParameters ImportECParameters(byte[] publicKey, byte[] privateKey)
        {
            return new ECParameters()
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = GetECPoint(publicKey),
                D = privateKey
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (byte[] PublicKey, byte[] PrivateKey) ExportECParameters(ECParameters parameters)
        {
            return (GetBytes(parameters.Q), parameters.D);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ECPoint GetECPoint(byte[] publicKey)
        {
            if(publicKey is null) return new ECPoint();

            return new ECPoint()
            {
                X = publicKey.AsSpan(1, 32).ToArray(),
                Y = publicKey.AsSpan(33, 32).ToArray()
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] GetBytes(ECPoint point)
        {
            var buffer = new byte[65];
            buffer[0] = 0x04;
            point.X.CopyTo(buffer.AsSpan(1));
            point.Y.CopyTo(buffer.AsSpan(33));
            return buffer;
        }

        public static byte[] DeriveKeyFromHmac(this ECDiffieHellman ecdh, byte[] otherPartyPublicKey, byte[] hmacKey)
        {
            if(ecdh is null) throw new ArgumentNullException(nameof(ecdh));
            if(otherPartyPublicKey is null) throw new ArgumentNullException(nameof(otherPartyPublicKey));
            if(hmacKey is null) throw new ArgumentNullException(nameof(hmacKey));

            using(var publicKey = ImportP256DHPublicKey(otherPartyPublicKey))
            {
                return ecdh.DeriveKeyFromHmac(publicKey, HashAlgorithmName.SHA256, hmacKey);
            }
        }

        public static byte[] ComputeHKDF(byte[] salt, byte[] ikm, byte[] data, int length)
        {
            if(data is null) throw new ArgumentNullException(nameof(data));

            using(var hmac = new HMACSHA256(salt))
            {
                var key = hmac.ComputeHash(ikm);
                var buffer = new byte[data.Length + 1];
                data.CopyTo(buffer, 0);
                buffer[^1] = 0x01;
                hmac.Key = key;
                hmac.Initialize();
                return hmac.ComputeHash(buffer).AsSpan(0, length).ToArray();
            }
        }

        public static (byte[] PublicKey, byte[] PrivateKey) GenerateP256ECKeys()
        {
            using(var ecdh = ECDiffieHellman.Create())
            {
                ecdh.GenerateKey(ECCurve.NamedCurves.nistP256);
                return ExportECParameters(ecdh.ExportParameters(true));
            }
        }
    }
}