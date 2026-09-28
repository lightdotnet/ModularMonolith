using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using StarterKit.WebMvc.Authentication;

namespace StarterKit.WebMvc.Services.Http;

/// <summary>
/// Attaches the current cookie session's access token as a <c>Bearer</c> header to every module
/// client call — the counterpart of the admin client's <c>bearerTokenHandler</c>. Requests
/// flagged <see cref="AnonymousOption"/> (the anonymous auth endpoints) are sent without one;
/// requests carrying <see cref="AccessTokenOption"/> use that token instead of the session's.
/// When the session token is known to be expired and could not be refreshed because the backend
/// is unavailable (<see cref="SessionRequestState.IsAccessTokenUnavailable"/>), the call is not sent
/// at all and answers a synthetic 503 — a transient failure the caller already handles — instead
/// of a 401 that would sign the user out over an outage.
/// </summary>
internal sealed class BearerTokenHandler(
    IHttpContextAccessor httpContextAccessor)
    : DelegatingHandler
{
    public static readonly HttpRequestOptionsKey<bool> AnonymousOption = new("StarterKit.WebMvc.Anonymous");

    public static readonly HttpRequestOptionsKey<string> AccessTokenOption = new("StarterKit.WebMvc.AccessToken");

    private const string UnavailableMessage = "The server is temporarily unavailable, so your session could not be renewed. Please try again shortly.";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var isAnonymous = request.Options.TryGetValue(AnonymousOption, out var anonymous) && anonymous;

        if (!isAnonymous)
        {
            string? accessToken;

            if (request.Options.TryGetValue(AccessTokenOption, out var explicitToken))
            {
                accessToken = explicitToken;
            }
            else
            {
                var httpContext = httpContextAccessor.HttpContext;

                if (httpContext is not null && SessionRequestState.IsAccessTokenUnavailable(httpContext))
                {
                    return ServiceUnavailable(request);
                }

                accessToken = await GetSessionAccessTokenAsync(httpContext);
            }

            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);
            }
        }

        return await base.SendAsync(
            request,
            cancellationToken);
    }

    private static async Task<string?> GetSessionAccessTokenAsync(HttpContext? httpContext)
    {
        if (httpContext is null)
        {
            return null;
        }

        // The cookie handler caches its authenticate result per request, so this reflects a
        // token refreshed earlier in the same request by SessionCookieEvents.
        return await httpContext.GetTokenAsync(
            SessionDefaults.AuthenticationScheme,
            SessionDefaults.AccessTokenName);
    }

    /// <summary>A backend-shaped failure envelope, so <see cref="ApiResponseReader"/> reads it like any other 503.</summary>
    private static HttpResponseMessage ServiceUnavailable(HttpRequestMessage request)
    {
        return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            RequestMessage = request,
            Content = JsonContent.Create(
                new
                {
                    code = ApiResultCodes.Error,
                    isSuccess = false,
                    message = UnavailableMessage,
                },
                options: ApiJson.Options),
        };
    }
}
