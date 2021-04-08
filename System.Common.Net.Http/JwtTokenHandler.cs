using System.Buffers;
using System.Buffers.Text;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Text.Encoding;

namespace System.Net.Http
{
    public class JwtTokenHandler : IDisposable
    {
        private readonly byte[] publicKey;
        private readonly byte[] privateKey;
        private readonly ECDsa ecdsa;
        private bool disposed;

        public JwtTokenHandler(byte[] publicKey, byte[] privateKey)
        {
            this.publicKey = publicKey;
            this.privateKey = privateKey;
            ecdsa = ECDsa.Create(CryptoExtensions.ImportECParameters(publicKey, privateKey));
        }

        public string Serialize(JwtToken token)
        {
            if(token is null) throw new ArgumentNullException(nameof(token));

            const DSASignatureFormat signatureFormat = DSASignatureFormat.IeeeP1363FixedFieldConcatenation;

            var jwtInfo = "{\"typ\":\"JWT\",\"alg\":\"ES256\"}";
            var maxJwtInfoSize = Base64.GetMaxEncodedToUtf8Length(jwtInfo.Length);
            var maxJwtDataSize = Base64.GetMaxEncodedToUtf8Length(
                2 + token.Claims.Sum(p => 6 + UTF8.GetMaxByteCount(p.Key.Length) + UTF8.GetMaxByteCount(p.Value.Length)));
            var maxJwtSignatureSize = Base64.GetMaxEncodedToUtf8Length(ecdsa.GetMaxSignatureSize(signatureFormat));

            // Encode JWT header part
            var bufWriter = new ArrayBufferWriter<byte>(maxJwtInfoSize + maxJwtDataSize + maxJwtSignatureSize + 2);
            var buffer = bufWriter.GetSpan(bufWriter.FreeCapacity);
            var total = UTF8.GetBytes(jwtInfo, buffer);
            total = Base64EncodeInPlace(buffer, total);
            buffer[total++] = 0x2E;
            bufWriter.Advance(total);

            // Encode JWT payload part
            using(var writer = new Utf8JsonWriter(bufWriter))
            {
                JsonSerializer.Serialize(writer, token.Claims, new JsonSerializerOptions()
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
            }
            total += Base64EncodeInPlace(buffer[total..], bufWriter.WrittenCount - total);
            buffer[total++] = 0x2E;

            // Sign and encode signature part
            if(ecdsa.TrySignData(buffer[0..(total - 1)], buffer[total..], HashAlgorithmName.SHA256, signatureFormat, out var bytesWritten))
            {
                total += Base64EncodeInPlace(buffer[total..], bytesWritten);
                return UTF8.GetString(buffer[..total]);
            }

            throw new InvalidOperationException("Signature computation failed");
        }

        protected virtual int Base64EncodeInPlace(Span<byte> buffer, int length)
        {
            Base64.EncodeToUtf8InPlace(buffer, length, out var written);
            Span<byte> encoded = buffer[..written].TrimEnd((byte)0x3D);

            for(int i = 0; i < encoded.Length; i++)
            {
                switch(encoded[i])
                {
                    case 0x2B:
                        encoded[i] = 0x2D;
                        break;
                    case 0x2F:
                        encoded[i] = 0x5F;
                        break;
                }
            }

            return encoded.Length;
        }

        protected virtual void Dispose(bool disposing)
        {
            if(!disposed)
            {
                if(disposing)
                {
                    ecdsa.Dispose();
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}