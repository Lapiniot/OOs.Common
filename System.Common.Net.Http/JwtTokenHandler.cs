using System.Buffers;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Text.Encoding;

namespace System.Net.Http;

public class JwtTokenHandler : IDisposable
{
    private readonly ECDsa ecdsa;
    private bool disposed;

    public JwtTokenHandler(byte[] publicKey, byte[] privateKey) =>
        ecdsa = ECDsa.Create(CryptoExtensions.ImportECParameters(publicKey, privateKey));

    public string Serialize(JwtToken token)
    {
        ArgumentNullException.ThrowIfNull(token);

        const DSASignatureFormat SignatureFormat = DSASignatureFormat.IeeeP1363FixedFieldConcatenation;
        const string JwtInfo = "{\"typ\":\"JWT\",\"alg\":\"ES256\"}";

        var maxJwtInfoSize = Base64.GetMaxEncodedToUtf8Length(JwtInfo.Length);
        var maxJwtDataSize = Base64.GetMaxEncodedToUtf8Length(
            2 + token.Claims.Sum(p => 6 + UTF8.GetMaxByteCount(p.Key.Length) + UTF8.GetMaxByteCount(p.Value.Length)));
        var maxJwtSignatureSize = Base64.GetMaxEncodedToUtf8Length(ecdsa.GetMaxSignatureSize(SignatureFormat));

        // Encode JWT header part
        var bufWriter = new ArrayBufferWriter<byte>(maxJwtInfoSize + maxJwtDataSize + maxJwtSignatureSize + 2);
        var buffer = bufWriter.GetSpan(bufWriter.FreeCapacity);
        var total = UTF8.GetBytes(JwtInfo, buffer);
        total = Base64EncodeInPlace(buffer, total);
        buffer[total++] = 0x2E;
        bufWriter.Advance(total);

        // Encode JWT payload part
        using (var writer = new Utf8JsonWriter(bufWriter))
        {
            JsonSerializer.Serialize(writer, token.Claims, new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }

        total += Base64EncodeInPlace(buffer[total..], bufWriter.WrittenCount - total);
        buffer[total++] = 0x2E;

        // Sign and encode signature part
        if (!ecdsa.TrySignData(buffer[..(total - 1)], buffer[total..], HashAlgorithmName.SHA256, SignatureFormat, out var bytesWritten))
        {
            throw new InvalidOperationException("Signature computation failed");
        }

        total += Base64EncodeInPlace(buffer[total..], bytesWritten);
        return UTF8.GetString(buffer[..total]);
    }

    protected virtual int Base64EncodeInPlace(Span<byte> buffer, int length)
    {
        _ = Base64.EncodeToUtf8InPlace(buffer, length, out var written);
        var encoded = buffer[..written].TrimEnd((byte)0x3D);

        for (var i = 0; i < encoded.Length; i++)
        {
            encoded[i] = encoded[i] switch
            {
                0x2B => 0x2D,
                0x2F => 0x5F,
                _ => encoded[i]
            };
        }

        return encoded.Length;
    }

    #region Implementation of IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if (disposed) return;

        if (disposing)
        {
            ecdsa.Dispose();
        }

        disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}