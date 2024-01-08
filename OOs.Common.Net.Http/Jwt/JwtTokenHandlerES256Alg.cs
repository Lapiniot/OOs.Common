using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.Json;
using static System.Text.Encoding;

namespace OOs.Net.Http.Jwt;

public sealed class JwtTokenHandlerES256Alg : IJwtTokenHandler, IDisposable
{
    // Contains Base64 encoded JWT header {"typ":"JWT","alg":"ES256"}
    private static ReadOnlySpan<byte> JwtHeaderEncoded => "eyJ0eXAiOiJKV1QiLCJhbGciOiJFUzI1NiJ9."u8;

    private readonly int initialBufferCapacity;
    private readonly ECDsa ecdsa;
    private bool disposed;

    public JwtTokenHandlerES256Alg(byte[] publicKey, byte[] privateKey, int initialBufferCapacity = 512)
    {
        ArgumentNullException.ThrowIfNull(publicKey);
        ArgumentNullException.ThrowIfNull(privateKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialBufferCapacity);

        ecdsa = ECDsa.Create(CryptoHelpers.ImportECParameters(publicKey, privateKey));
        this.initialBufferCapacity = initialBufferCapacity;
    }

    public string Write(JwtToken token)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(token);

        const DSASignatureFormat SignatureFormat = DSASignatureFormat.IeeeP1363FixedFieldConcatenation;

        var bufWriter = new ArrayBufferWriter<byte>(initialBufferCapacity);

        // Write pre-encoded fixed JWT header part
        var headerCount = JwtHeaderEncoded.Length;
        var buffer = bufWriter.GetSpan(headerCount);
        JwtHeaderEncoded.CopyTo(buffer);
        bufWriter.Advance(headerCount);

        // Encode JWT payload part
        using (var writer = new Utf8JsonWriter(bufWriter))
        {
            JsonSerializer.Serialize(writer, token.Claims, JsonContext.Default.DictionaryStringObject);
        }

        var payloadCount = bufWriter.WrittenCount - headerCount;
        bufWriter.ResetWrittenCount();
        bufWriter.Advance(headerCount);
        buffer = bufWriter.GetSpan(Base64.GetMaxEncodedToUtf8Length(payloadCount) + 1);
        Base64UrlSafe.EncodeToUtf8InPlace(buffer: buffer, dataLength: payloadCount, bytesWritten: out payloadCount);
        buffer[payloadCount++] = 0x2E;

        // Sign and encode signature part
        bufWriter.ResetWrittenCount();
        var dataCount = headerCount + payloadCount;
        buffer = bufWriter.GetSpan(dataCount + Base64.GetMaxEncodedToUtf8Length(ecdsa.GetMaxSignatureSize(SignatureFormat)));
        var dataSpan = buffer.Slice(0, dataCount - 1);
        var signatureSpan = buffer.Slice(dataCount);
        if (!ecdsa.TrySignData(dataSpan, signatureSpan, HashAlgorithmName.SHA256, SignatureFormat, out var signatureCount))
            ThrowSignatureComputeFailed();

        Base64UrlSafe.EncodeToUtf8InPlace(signatureSpan, signatureCount, out signatureCount);
        var count = dataCount + signatureCount;
        bufWriter.Advance(count);

        return UTF8.GetString(bufWriter.WrittenSpan);
    }

    [DoesNotReturn]
    private static void ThrowSignatureComputeFailed() => throw new InvalidOperationException("Signature computation failed");

    #region Implementation of IDisposable

    public void Dispose()
    {
        if (disposed) return;
        ecdsa.Dispose();
        disposed = true;
    }

    #endregion
}