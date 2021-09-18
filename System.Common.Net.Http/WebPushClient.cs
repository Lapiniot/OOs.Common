using System.Buffers.Binary;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using static System.Text.Encoding;

namespace System.Net.Http;

public class WebPushClient : IDisposable
{
    private readonly HttpClient client;
    private readonly byte[] publicKey;
    private readonly byte[] privateKey;
    private readonly string subject;
    private readonly int expires;
    private readonly JwtTokenHandler tokenHandler;
    private readonly string cryptoKey;
    private bool disposed;

    public WebPushClient(HttpClient client, byte[] publicKey, byte[] privateKey, string jwtSubject, int jwtExpires)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(publicKey);
        ArgumentNullException.ThrowIfNull(privateKey);

        if(string.IsNullOrEmpty(jwtSubject)) throw new ArgumentException($"'{nameof(jwtSubject)}' cannot be null or empty.", nameof(jwtSubject));
        if(jwtExpires <= 0) throw new ArgumentException($"{nameof(jwtExpires)} must be greater than zero");

        this.client = client;
        this.publicKey = publicKey;
        this.privateKey = privateKey;
        subject = jwtSubject;
        expires = jwtExpires;
        tokenHandler = new JwtTokenHandler(this.publicKey, this.privateKey);
        cryptoKey = Encoders.ToBase64String(publicKey);
    }

    public async Task SendAsync(Uri endpoint, SubscriptionKeys keys, byte[] payload, int ttl, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(payload);

        if(ttl <= 0) throw new ArgumentException($"{nameof(ttl)} must be greater then zero");

        var token = new JwtToken()
        {
            Audience = endpoint.GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped),
            Subject = subject,
            Expires = DateTimeOffset.UtcNow.AddSeconds(expires)
        };

        var salt = CryptoExtensions.GenerateSalt(16);
        var clientPublicKey = keys.P256DHKey;
        var (serverPublicKey, derivedKeyMaterial) = GenerateServerKeys(clientPublicKey, keys.AuthKey);
        var prk = GeneratePseudoRandomKey(derivedKeyMaterial);
        var encryptionKey = CryptoExtensions.ComputeHKDF(salt, prk, CreateInfo("aesgcm", clientPublicKey, serverPublicKey), 16);
        var nonce = CryptoExtensions.ComputeHKDF(salt, prk, CreateInfo("nonce", clientPublicKey, serverPublicKey), 12);
        var data = GetPayloadWithPadding(payload);
        var encrypted = EncryptPayload(data, encryptionKey, nonce);

        using var content = CreateHttpContent(encrypted);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Headers =
                {
                    { "Authorization", $"WebPush {tokenHandler.Serialize(token)}" },
                    { "Encryption", $"salt={Encoders.ToBase64String(salt)}" },
                    { "Crypto-Key", $"dh={Encoders.ToBase64String(serverPublicKey)}; p256ecdsa={cryptoKey}" },
                    { "TTL", ttl.ToString(CultureInfo.InvariantCulture) }
                },
            Content = content
        };
        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();
    }

    private static HttpContent CreateHttpContent(byte[] content)
    {
        return new ByteArrayContent(content)
        {
            Headers =
                {
                    ContentType = new MediaTypeHeaderValue("application/octet-stream"),
                    ContentEncoding = { "aesgcm" }
                }
        };
    }

    private static byte[] EncryptPayload(byte[] data, byte[] key, byte[] nonce)
    {
        using var aes = new AesGcm(key);
        byte[] cipher = new byte[data.Length + AesGcm.TagByteSizes.MaxSize];
        aes.Encrypt((Span<byte>)nonce, data, cipher.AsSpan(0, data.Length), cipher.AsSpan(data.Length));
        return cipher;
    }

    private static (byte[] PublicKey, byte[] DerivedKeyMaterial) GenerateServerKeys(byte[] otherPartyPublicKey, byte[] hmacKey)
    {
        using var ecdh = ECDiffieHellman.Create();
        ecdh.GenerateKey(ECCurve.NamedCurves.nistP256);
        var publicKey = ecdh.ExportP256DHPublicKey();
        var keyMaterial = ecdh.DeriveKeyFromHmac(otherPartyPublicKey, hmacKey);
        return (publicKey, keyMaterial);
    }

    private static byte[] GeneratePseudoRandomKey(byte[] key)
    {
        const string str = "Content-Encoding: auth\0";
        Span<byte> buffer = stackalloc byte[str.Length + 1];
        _ = ASCII.GetBytes(str, buffer);
        buffer[^1] = 0x01;
        return HMACSHA256.HashData(key, buffer);
    }

    private static byte[] GetPayloadWithPadding(byte[] payload)
    {
        var paddingLength = (((payload.Length / 16) + 1) * 16) - payload.Length;
        byte[] data = new byte[2 + paddingLength + payload.Length];
        Span<byte> span = data;
        span[..(2 + paddingLength)].Fill(0);
        BinaryPrimitives.WriteUInt16BigEndian(span, (ushort)paddingLength);
        payload.CopyTo(span[(2 + paddingLength)..]);
        return data;
    }

    private static byte[] CreateInfo(string label, byte[] clientPublicKey, byte[] serverPublicKey)
    {
        int len = label.Length;
        var buffer = new byte[18 + len + 1 + 5 + 1 + 2 + clientPublicKey.Length + 2 + serverPublicKey.Length];
        var span = buffer.AsSpan();
        _ = UTF8.GetBytes("Content-Encoding: ", span);
        _ = UTF8.GetBytes(label, span[18..]);
        span[18 + len] = 0;
        _ = UTF8.GetBytes("P-256", span[(19 + len)..]);
        span[24 + len] = 0;
        BinaryPrimitives.WriteUInt16BigEndian(span[(25 + len)..], (ushort)clientPublicKey.Length);
        clientPublicKey.CopyTo(span[(27 + len)..]);
        BinaryPrimitives.WriteUInt16BigEndian(span[(27 + len + clientPublicKey.Length)..], (ushort)serverPublicKey.Length);
        serverPublicKey.CopyTo(span[(29 + len + clientPublicKey.Length)..]);
        return buffer;
    }

    protected virtual void Dispose(bool disposing)
    {
        if(!disposed)
        {
            if(disposing)
            {
                tokenHandler.Dispose();
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