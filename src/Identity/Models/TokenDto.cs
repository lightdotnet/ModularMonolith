namespace StarterKit.Modules.Identity.Models;

public record TokenDto(string AccessToken, long ExpiresIn, string? RefreshToken);
