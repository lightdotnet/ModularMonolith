using Light.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace Monolith.Blazor.Services;

public class JwtServerAuthenticationStateProvider(
    ILoggerFactory loggerFactory)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(5);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        var user = authenticationState.User;

        if (user.Identity?.IsAuthenticated != true)
            return false;

        // Read expiry claim
        var expClaim = user.FindFirst(ClaimTypes.Expiration)?.Value;
        if (!long.TryParse(expClaim, out var expUnix))
            return false;

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnix);

        // check if the token is expired or about to expire in the next 5 minutes
        return expiresAt.AddMinutes(-10) > DateTimeOffset.UtcNow;
    }
}