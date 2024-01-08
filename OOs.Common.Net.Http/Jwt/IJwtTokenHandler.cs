namespace OOs.Net.Http.Jwt;

public interface IJwtTokenHandler
{
    string Write(JwtToken token);
}