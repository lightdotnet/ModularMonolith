using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using StarterKit.Identity.Contracts;
using StarterKit.WebMvc.Services.Http;
using StarterKit.WebMvc.Services.Identity;

namespace StarterKit.WebMvc.Authentication;

/// <summary>
/// Keeps the cookie session fresh on every authenticated request — the server-side port of the
/// admin client's <c>proxy.ts</c> (hard session cap) + <c>ensure-fresh-session-action.ts</c> /
/// <c>refresh-session.ts</c> (token refresh):
/// <list type="bullet">
/// <item>past the 7-day session cap → sign out;</item>
/// <item>access token within <see cref="SessionDefaults.RefreshLead"/> of expiry → rotate it via
/// <c>auth/token/refresh</c>, re-decode roles/permissions from the new JWT, refetch the profile and
/// reissue the cookie — the only path that rewrites the cookie;</item>
/// <item>a permanent refresh failure (401/400) leaves the ticket untouched — the cookie is never
/// reissued on failure, because a parallel request (another tab) may just have rotated the
/// refresh token and written a newer cookie that a reissue would overwrite with the stale tokens.
/// The principal is only rejected when the failure count stored in the cookie would reach
/// <see cref="SessionDefaults.MaxRefreshFailures"/>. Since a failure no longer rewrites the cookie,
/// a genuinely dead session ends when its access token expires: the backend then answers 401 and
/// <c>ApiAuthorizationExceptionMiddleware</c> signs the user out;</item>
/// <item>a transient failure (network/5xx) says nothing about the token and never counts. If the
/// access token has already expired, the request is marked with
/// <see cref="SessionRequestState.MarkAccessTokenUnavailable"/> so backend calls fail as
/// "service unavailable" rather than sending the dead token and signing the user out.</item>
/// </list>
/// </summary>
internal sealed class SessionCookieEvents(
    IAuthClient authClient,
    IUserProfileClient userProfileClient,
    TimeProvider timeProvider,
    ILogger<SessionCookieEvents> logger)
    : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var now = timeProvider.GetUtcNow();
        var ticket = SessionTicket.Read(context.Properties);

        if (ticket is null || ticket.SessionExpiresAt <= now)
        {
            await RejectAsync(
                context,
                "session missing or past its lifetime cap");

            return;
        }

        if (ticket.AccessTokenExpiresAt - now > SessionDefaults.RefreshLead)
        {
            return;
        }

        if (string.IsNullOrEmpty(ticket.RefreshToken))
        {
            await HandlePermanentFailureAsync(
                context,
                ticket);

            return;
        }

        var cancellationToken = context.HttpContext.RequestAborted;

        var result = await authClient.RefreshTokenAsync(
            new RefreshTokenRequest(
                ticket.AccessToken,
                ticket.RefreshToken),
            cancellationToken);

        if (result is { IsSuccess: true, Data: not null })
        {
            await ApplyRefreshedTokenAsync(
                context,
                ticket,
                result.Data,
                now,
                cancellationToken);

            return;
        }

        if (result.Code is ApiResultCodes.Unauthorized or ApiResultCodes.BadRequest)
        {
            await HandlePermanentFailureAsync(
                context,
                ticket);

            return;
        }

        if (ticket.AccessTokenExpiresAt <= now)
        {
            SessionRequestState.MarkAccessTokenUnavailable(context.HttpContext);
        }

        logger.LogInformation(
            "Session refresh failed transiently ({Code}: {Message}); keeping the current token (expired: {Expired}).",
            result.Code,
            result.Message,
            ticket.AccessTokenExpiresAt <= now);
    }

    private async Task ApplyRefreshedTokenAsync(
        CookieValidatePrincipalContext context,
        SessionTicket ticket,
        TokenDto token,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        // Roles/permissions live in the JWT, so the principal is rebuilt from the new token; the
        // profile is refetched like the admin client's refetchProfile, falling back to the
        // previously held profile claims if that call fails.
        var profileResult = await userProfileClient.GetAsync(
            token.AccessToken,
            cancellationToken);

        var principal = SessionPrincipalFactory.Create(
            token.AccessToken,
            profileResult.IsSuccess ? profileResult.Data : null,
            SessionPrincipalFactory.NonTokenClaims(
                context.Principal!,
                ticket.AccessToken));

        var refreshed = ticket with
        {
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            AccessTokenExpiresAt = now.AddSeconds(token.ExpiresIn),
            RefreshFailureCount = 0,
        };

        refreshed.WriteTo(
            context.Properties,
            now);

        context.ReplacePrincipal(principal);
        context.ShouldRenew = true;

        logger.LogDebug("Session access token refreshed.");
    }

    private async Task HandlePermanentFailureAsync(
        CookieValidatePrincipalContext context,
        SessionTicket ticket)
    {
        var failureCount = ticket.RefreshFailureCount + 1;

        if (failureCount >= SessionDefaults.MaxRefreshFailures)
        {
            await RejectAsync(
                context,
                $"{failureCount} consecutive permanent refresh failures");

            return;
        }

        // Deliberately no ShouldRenew: see the class remarks on the multi-tab rotation race.
        logger.LogInformation(
            "Session refresh was rejected ({FailureCount}/{Max}); leaving the session cookie untouched.",
            failureCount,
            SessionDefaults.MaxRefreshFailures);
    }

    private async Task RejectAsync(
        CookieValidatePrincipalContext context,
        string reason)
    {
        logger.LogInformation(
            "Signing out cookie session: {Reason}.",
            reason);

        context.RejectPrincipal();

        await context.HttpContext.SignOutAsync(SessionDefaults.AuthenticationScheme);
    }
}
