namespace Kioskitos.Infrastructure.Auth;

public sealed class JwtOptions
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public int ExpirationMinutes { get; set; } = 60;
}

public sealed class GoogleOptions
{
    public string ClientId { get; set; } = "";
}