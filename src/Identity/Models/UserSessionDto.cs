namespace StarterKit.Modules.Identity.Models;

public class UserSessionDto
{
    public string Id { get; set; } = null!;

    public DateTimeOffset? ExpiresAt { get; set; }

    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }

    public DeviceDto? Device { get; set; }
}
