using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using StarterKit.Identity.Api.Jwt;
using StarterKit.Notifications.Contracts.SystemNotifications;
using System.IdentityModel.Tokens.Jwt;

namespace StarterKit.WebApi.Authentication;

/// <summary>
/// Co-host only. Keeps the dedicated short-lived SignalR hub token (identified by its
/// hub-only <c>aud</c> claim) out of the regular JSON API.
///
/// Defense in depth: the primary (fail-closed) control is the dedicated <c>"HubBearer"</c>
/// JwtBearer sub-scheme in <see cref="ApiAuthenticationExtensions"/>, which validates the
/// audience so a hub token never authenticates against the main <c>"Bearer"</c> scheme.
/// This handler is the fail-open backstop for the same direction: if audience validation is
/// ever weakened, a hub token that still authenticates is rejected here on any path outside
/// the hub. The hub token also carries no role or permission claims, so every
/// <c>[MustHavePermission]</c> endpoint already rejects it; this handler additionally covers
/// bare <c>[Authorize]</c> endpoints. It runs on every authenticated endpoint and fails
/// authorization whenever a hub token is presented anywhere other than the hub path.
/// </summary>
internal sealed class HubTokenApiGuardHandler(
    IHttpContextAccessor httpContextAccessor,
    IOptions<JwtOptions> jwtOptions)
    : IAuthorizationHandler
{
    private readonly string _hubAudience = jwtOptions.Value.HubAudience;

    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        var isHubToken = context.User.HasClaim(
            JwtRegisteredClaimNames.Aud,
            _hubAudience);

        if (!isHubToken)
            return Task.CompletedTask;

        var isHubRequest = httpContextAccessor.HttpContext?.Request
            .Path.StartsWithSegments(NotificationConstants.HubPath) is true;

        if (!isHubRequest)
        {
            context.Fail(new AuthorizationFailureReason(
                this,
                "Hub tokens are not valid for API endpoints."));
        }

        return Task.CompletedTask;
    }
}
