namespace Monolith.Identity;

public record TokenDto(
    string AccessToken,
    long ExpiresIn,
    string? RefreshToken);