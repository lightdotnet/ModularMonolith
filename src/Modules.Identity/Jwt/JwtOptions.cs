namespace Monolith.Identity.Jwt;

public class JwtOptions
{
    public string Issuer { get; set; } = "https://localhost";

    public string SecretKey { get; set; } = "3CC79718-C525-4F1A-AFCF-E9F3722C6008"; // must length > 18

    public int AccessTokenExpirationSeconds { get; set; } = 86400; // 1 days

    public int RefreshTokenExpirationDays { get; set; } = 7; // 7 days
}