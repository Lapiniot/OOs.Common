using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using OOs.Net.Http.Jwt;
using static System.Text.Encoding;

namespace OOs.Net.Http.WebPush;

public class WebPushClient
{
    private readonly HttpClient client;
    private readonly string cryptoKey;
    private readonly int jwtExpiresSeconds;
    private readonly string jwtSubject;
    private readonly IJwtTokenHandler jwtTokenHandler;

    public WebPushClient(HttpClient client, byte[] serverPublicKey, IJwtTokenHandler jwtTokenHandler, string jwtSubject, int jwtExpiresSeconds)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(serverPublicKey);
        ArgumentNullException.ThrowIfNull(jwtTokenHandler);
        ArgumentNullException.ThrowIfNull(jwtSubject);
        ArgumentOutOfRangeException.ThrowIfZero(jwtSubject.Length);
        ArgumentOutOfRangeException.ThrowIfLessThan(jwtExpiresSeconds, 1);

        this.client = client;
        this.jwtTokenHandler = jwtTokenHandler;
        this.jwtSubject = jwtSubject;
        this.jwtExpiresSeconds = jwtExpiresSeconds;
        cryptoKey = Base64UrlSafe.ToBase64String(serverPublicKey);
    }

    public async Task SendAsync(Uri endpoint, byte[] clientPublicKey, byte[] authKey, byte[] payload, int ttl, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(clientPublicKey);
        ArgumentNullException.ThrowIfNull(authKey);
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentOutOfRangeException.ThrowIfLessThan(ttl, 1);

        var token = new JwtToken
        {
            Audience = endpoint.GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped),
            Subject = jwtSubject,
            Expires = DateTimeOffset.UtcNow.AddSeconds(jwtExpiresSeconds)
        };

        var salt = CryptoHelpers.GenerateSalt(16);
        var (serverPublicKey, derivedKeyMaterial) = GenerateServerKeys(clientPublicKey, authKey);
        var prk = GeneratePseudoRandomKey(derivedKeyMaterial);
        var encryptionKey = CryptoHelpers.ComputeHKDF(salt, prk, CreateInfo("aesgcm", clientPublicKey, serverPublicKey), 16);
        var nonce = CryptoHelpers.ComputeHKDF(salt, prk, CreateInfo("nonce", clientPublicKey, serverPublicKey), 12);
        var data = GetPayloadWithPadding(payload);
        var encrypted = EncryptPayload(data, encryptionKey, nonce);

        using var content = CreateHttpContent(encrypted);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Headers =
            {
                { "Authorization", $"WebPush {jwtTokenHandler.Write(token)}" },
                { "Encryption", $"salt={Base64UrlSafe.ToBase64String(salt)}" },
                { "Crypto-Key", $"dh={Base64UrlSafe.ToBase64String(serverPublicKey)}; p256ecdsa={cryptoKey}" },
                { "TTL", ttl.ToString(CultureInfo.InvariantCulture) }
            },
            Content = content
        };

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
    }

    private static ByteArrayContent CreateHttpContent(byte[] content) =>
        new(content)
        {
            Headers =
            {
                ContentType = new("application/octet-stream"),
                ContentEncoding = { "aesgcm" }
            }
        };

    private static byte[] EncryptPayload(byte[] data, byte[] key, byte[] nonce)
    {
        using var aes = new AesGcm(key, 16);
        var cipher = new byte[data.Length + AesGcm.TagByteSizes.MaxSize];
        aes.Encrypt((Span<byte>)nonce, data, cipher.AsSpan(0, data.Length), cipher.AsSpan(data.Length));
        return cipher;
    }

    private static (byte[] PublicKey, byte[] DerivedKeyMaterial) GenerateServerKeys(byte[] otherPartyPublicKey, byte[] hmacKey)
    {
        using var ecdh = ECDiffieHellman.Create();
        ecdh.GenerateKey(ECCurve.NamedCurves.nistP256);
        var publicKey = CryptoHelpers.GetBytes(ecdh.PublicKey.ExportParameters().Q);
        var keyMaterial = CryptoHelpers.DeriveKeyFromHmac(ecdh, otherPartyPublicKey, hmacKey);
        return (publicKey, keyMaterial);
    }

    private static byte[] GeneratePseudoRandomKey(byte[] key)
    {
        const string HeaderStr = "Content-Encoding: auth\0";
        Span<byte> buffer = stackalloc byte[HeaderStr.Length + 1];
        ASCII.GetBytes(HeaderStr, buffer);
        buffer[^1] = 0x01;
        return HMACSHA256.HashData(key, buffer);
    }

    private static byte[] GetPayloadWithPadding(byte[] payload)
    {
        var paddingLength = (payload.Length / 16 + 1) * 16 - payload.Length;
        var data = new byte[2 + paddingLength + payload.Length];
        Span<byte> span = data;
        span.Slice(0, 2 + paddingLength).Clear();
        BinaryPrimitives.WriteUInt16BigEndian(span, (ushort)paddingLength);
        payload.CopyTo(span.Slice(2 + paddingLength));
        return data;
    }

    private static byte[] CreateInfo(string label, byte[] clientPublicKey, byte[] serverPublicKey)
    {
        var len = label.Length;
        var buffer = new byte[18 + len + 1 + 5 + 1 + 2 + clientPublicKey.Length + 2 + serverPublicKey.Length];
        var span = buffer.AsSpan();
        var header = "Content-Encoding: "u8;
        header.CopyTo(span);
        UTF8.GetBytes(label, span.Slice(18));
        span[18 + len] = 0;
        var p256 = "P-256"u8;
        p256.CopyTo(span.Slice(19 + len));
        span[24 + len] = 0;
        BinaryPrimitives.WriteUInt16BigEndian(span.Slice(25 + len), (ushort)clientPublicKey.Length);
        clientPublicKey.CopyTo(span.Slice(27 + len));
        BinaryPrimitives.WriteUInt16BigEndian(span.Slice(27 + len + clientPublicKey.Length), (ushort)serverPublicKey.Length);
        serverPublicKey.CopyTo(span.Slice(29 + len + clientPublicKey.Length));
        return buffer;
    }
}