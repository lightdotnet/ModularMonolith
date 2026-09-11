namespace StarterKit.Identity.Api.Jwt;

public class JwtOptions
{
    public string Issuer { get; set; } = "https://localhost";

    public string SecretKey { get; set; } = string.Empty; // must length > 18; bound from configuration, no committed default

    public int AccessTokenExpirationSeconds { get; set; } = 7200; // 2 hours

    public int RefreshTokenExpirationDays { get; set; } = 7; // 7 days

    public int HubTokenExpirationSeconds { get; set; } = 120; // 2 minutes - short-lived SignalR handshake token

    public string HubAudience { get; set; } = "signalr-hub"; // audience stamped on the hub token, absent from the full API token
}