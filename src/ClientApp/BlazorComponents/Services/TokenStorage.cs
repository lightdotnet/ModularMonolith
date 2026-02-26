using System.Text.Json;

namespace Monolith.Blazor.Services;

public abstract class TokenStorage
{
    public abstract Task<TokenModel?> GetAsync();

    public abstract Task SaveAsync(TokenModel token);

    public abstract Task ClearAsync();
}

public class TokenModel
{
    public TokenModel()
    { }

    public TokenModel(string accessToken, long expireInSeconds, string? refreshToken)
    {
        Token = accessToken;
        ExpireOn = DateTimeOffset.Now.AddSeconds(expireInSeconds);

        if (refreshToken != null)
        {
            RefreshToken = refreshToken;
            RefreshTokenExpireOn = DateTimeOffset.Now.AddDays(7);
        }

        //ExpireOn = DateTime.UtcNow.AddSeconds(15);
        //RefreshTokenExpireOn = DateTime.UtcNow.AddSeconds(30);
    }

    public string Token { get; set; } = "";

    public DateTimeOffset ExpireOn { get; set; }

    public string? RefreshToken { get; set; } = "";

    public DateTimeOffset? RefreshTokenExpireOn { get; set; }

    public bool IsExpiringSoon()
    {
        var tokenExpirationBuffer = TimeSpan.FromMinutes(10);

        var timeUntilExpiration = ExpireOn - DateTimeOffset.UtcNow;

        return timeUntilExpiration <= tokenExpirationBuffer;
    }

    public bool IsRefreshTokenExpired() => RefreshTokenExpireOn <= DateTime.UtcNow;

    public override string ToString()
    {
        var json = JsonSerializer.Serialize(this);
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(plainTextBytes);
    }

    public static TokenModel? ReadFrom(string? base64EncodedData)
    {
        if (string.IsNullOrEmpty(base64EncodedData)) return null;

        try
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            var data = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            return JsonSerializer.Deserialize<TokenModel>(data);
        }
        catch
        {
            return null;
        }
    }
}