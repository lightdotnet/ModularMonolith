using Microsoft.AspNetCore.Authentication;
using StarterKit.Identity.Contracts;
using StarterKit.WebMvc.Services.Identity;

namespace StarterKit.WebMvc.Authentication;

/// <summary>
/// Establishes the cookie session from a freshly issued token. Shared by every sign-in path that
/// ends with a <see cref="TokenDto"/> (password/AD login, Microsoft external login) so the
/// resulting session is identical regardless of how the token was obtained — the port of the
/// admin client's <c>establishSession</c>.
/// </summary>
public interface ISessionSignInService
{
    Task SignInAsync(
        HttpContext httpContext,
        TokenDto token,
        CancellationToken cancellationToken = default);
}

internal sealed class SessionSignInService(
    IUserProfileClient userProfileClient,
    TimeProvider timeProvider)
    : ISessionSignInService
{
    public async Task SignInAsync(
        HttpContext httpContext,
        TokenDto token,
        CancellationToken cancellationToken = default)
    {
        // A profile fetch failure must not block an otherwise successful sign-in — start the
        // session with the JWT claims alone, as the admin client does.
        var profileResult = await userProfileClient.GetAsync(
            token.AccessToken,
            cancellationToken);

        var principal = SessionPrincipalFactory.Create(
            token.AccessToken,
            profileResult.IsSuccess ? profileResult.Data : null);

        var now = timeProvider.GetUtcNow();

        var ticket = new SessionTicket(
            token.AccessToken,
            token.RefreshToken,
            now.AddSeconds(token.ExpiresIn),
            now.Add(SessionDefaults.SessionLifetime),
            RefreshFailureCount: 0);

        var properties = new AuthenticationProperties();
        ticket.WriteTo(
            properties,
            now);

        await httpContext.SignInAsync(
            SessionDefaults.AuthenticationScheme,
            principal,
            properties);
    }
}
