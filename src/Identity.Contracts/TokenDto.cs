namespace StarterKit.Identity.Contracts;

public record TokenDto(
    string AccessToken,
    long ExpiresIn,
    string? RefreshToken);