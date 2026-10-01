namespace StarterKit.Modules.Identity.Application.Common.Models;

public record TokenDto(string AccessToken, long ExpiresIn, string? RefreshToken);
