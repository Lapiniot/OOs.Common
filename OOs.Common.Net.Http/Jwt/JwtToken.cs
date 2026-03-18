namespace OOs.Net.Http.Jwt;

public sealed class JwtToken
{
    private readonly Dictionary<string, object?> claims;

    public JwtToken() => claims = [];

    public JwtToken(IReadOnlyDictionary<string, object?> claims) => this.claims = new(claims);

    public IReadOnlyDictionary<string, object?> Claims => claims;

    public string? Issuer
    {
        get => claims.TryGetValue("iss", out var value) ? value as string : null;
        set => claims["iss"] = value;
    }

    public string? Audience
    {
        get => claims.TryGetValue("aud", out var value) ? value as string : null;
        set => claims["aud"] = value;
    }

    public string? Subject
    {
        get => claims.TryGetValue("sub", out var value) ? value as string : null;
        set => claims["sub"] = value;
    }

    public DateTimeOffset? Expires
    {
        get => claims.TryGetValue("exp", out var value) && value is long v
            ? DateTimeOffset.FromUnixTimeSeconds(v).ToUniversalTime()
            : null;
        set => claims["exp"] = value?.ToUniversalTime().ToUnixTimeSeconds();
    }
}