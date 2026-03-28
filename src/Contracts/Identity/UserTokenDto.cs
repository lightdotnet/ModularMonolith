namespace Monolith.Identity;

public class UserTokenDto
{
    public string Id { get; set; } = null!;

    public DateTimeOffset? ExpiresAt { get; set; }

    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }

    public DeviceDto? Device { get; set; }
}