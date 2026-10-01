namespace StarterKit.Modules.Identity.Application.Common.Models;

public record RefreshTokenRequest(string AccessToken, string RefreshToken);
