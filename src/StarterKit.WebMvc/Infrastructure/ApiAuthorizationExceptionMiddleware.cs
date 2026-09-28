using Microsoft.AspNetCore.Authentication;
using StarterKit.WebMvc.Authentication;
using StarterKit.WebMvc.Services.Http;

namespace StarterKit.WebMvc.Infrastructure;

/// <summary>
/// Turns an <see cref="ApiAuthorizationException"/> thrown anywhere in a controller, page, view or
/// view component into the matching auth outcome: 401 → sign out, then challenge (redirect to the
/// login page with a return URL); 403 → forbid (redirect to the access-denied page). For fetch
/// requests (<c>X-Requested-With: XMLHttpRequest</c>) the cookie handler answers with the bare
/// 401/403 status instead of a redirect.
/// </summary>
internal sealed class ApiAuthorizationExceptionMiddleware(
    RequestDelegate next,
    ILogger<ApiAuthorizationExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiAuthorizationException ex) when (!context.Response.HasStarted)
        {
            logger.LogInformation(
                "Backend returned {StatusCode} for {Path}: {Message}",
                ex.StatusCode,
                context.Request.Path,
                ex.Message);

            context.Response.Clear();

            if (ex.StatusCode == StatusCodes.Status401Unauthorized)
            {
                await context.SignOutAsync(SessionDefaults.AuthenticationScheme);
                await context.ChallengeAsync(SessionDefaults.AuthenticationScheme);
                return;
            }

            await context.ForbidAsync(SessionDefaults.AuthenticationScheme);
        }
    }
}
