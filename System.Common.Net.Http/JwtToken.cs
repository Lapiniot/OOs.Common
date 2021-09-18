using System.Globalization;

namespace System.Net.Http;

public class JwtToken
{
    private readonly Dictionary<string, string> claims;

    public JwtToken()
    {
        this.claims = new Dictionary<string, string>();
    }

    public JwtToken(IReadOnlyDictionary<string, string> claims)
    {
        this.claims = new Dictionary<string, string>(claims);
    }

    public string Issuer
    {
        get => claims.TryGetValue("iss", out var value) ? value : null;
        set => claims["iss"] = value;
    }

    public string Audience
    {
        get => claims.TryGetValue("aud", out var value) ? value : null;
        set => claims["aud"] = value;
    }

    public string Subject
    {
        get => claims.TryGetValue("sub", out var value) ? value : null;
        set => claims["sub"] = value;
    }

    public DateTimeOffset? Expires
    {
        get => claims.TryGetValue("exp", out var value) && int.TryParse(value, out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds).ToUniversalTime()
            : null;
        set => claims["exp"] = value is not null
            ? value.Value.ToUniversalTime().ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)
            : null;
    }

    public Dictionary<string, string> Claims { get => claims; }
}