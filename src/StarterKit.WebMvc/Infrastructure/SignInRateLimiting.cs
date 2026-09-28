using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using StarterKit.WebMvc.Authentication;

namespace StarterKit.WebMvc.Infrastructure;

/// <summary>
/// Per-client-IP fixed-window limits on sign-in attempts (ASP.NET Core rate limiter), configured
/// from <see cref="SignInRateLimitOptions"/>. <see cref="PasswordPolicy"/> only counts POSTs, so
/// showing the login page is never limited; <see cref="ExternalPolicy"/> counts every start of the
/// Microsoft handshake. A rejected browser request is redirected (303) to the login page with a
/// friendly message; a fetch request gets a bare 429 with a JSON message. The client IP is the
/// one resolved by <c>UseForwardedHeaders</c>, so trusted proxies must be configured.
/// </summary>
public static class SignInRateLimiting
{
    public const string PasswordPolicy = "sign-in-password";

    public const string ExternalPolicy = "sign-in-external";

    private const string RejectedMessage = "Too many sign-in attempts. Please wait a minute and try again.";

    public static IServiceCollection AddSignInRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(SignInRateLimitOptions.SectionName).Get<SignInRateLimitOptions>()
            ?? new SignInRateLimitOptions();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiter.AddPolicy(
                PasswordPolicy,
                httpContext => HttpMethods.IsPost(httpContext.Request.Method)
                    ? ClientIpPartition(
                        httpContext,
                        options)
                    : RateLimitPartition.GetNoLimiter(string.Empty));

            limiter.AddPolicy(
                ExternalPolicy,
                httpContext => ClientIpPartition(
                    httpContext,
                    options));

            limiter.OnRejected = OnRejectedAsync;
        });

        return services;
    }

    private static RateLimitPartition<string> ClientIpPartition(
        HttpContext httpContext,
        SignInRateLimitOptions options)
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = Math.Max(options.PermitLimit, 1),
                Window = TimeSpan.FromSeconds(Math.Max(options.WindowSeconds, 1)),
                QueueLimit = 0,
                AutoReplenishment = true,
            });
    }

    private static async ValueTask OnRejectedAsync(
        OnRejectedContext context,
        CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;

        httpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(SignInRateLimiting))
            .LogWarning(
                "Sign-in rate limit hit by {ClientIp} on {Method} {Path}.",
                httpContext.Connection.RemoteIpAddress,
                httpContext.Request.Method,
                httpContext.Request.Path);

        if (context.Lease.TryGetMetadata(
            MetadataName.RetryAfter,
            out var retryAfter))
        {
            httpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        if (httpContext.Request.Headers.XRequestedWith == "XMLHttpRequest")
        {
            await httpContext.Response.WriteAsJsonAsync(
                new { message = RejectedMessage },
                cancellationToken);

            return;
        }

        var tempData = httpContext.RequestServices
            .GetRequiredService<ITempDataDictionaryFactory>()
            .GetTempData(httpContext);

        SignInErrorMessage.Set(
            tempData,
            RejectedMessage);

        tempData.Save();

        var loginUrl = QueryHelpers.AddQueryString(
            $"{httpContext.Request.PathBase}{SessionDefaults.LoginPath}",
            "returnUrl",
            SafeReturnUrl.Sanitize(await ReadReturnUrlAsync(
                httpContext.Request,
                cancellationToken)));

        httpContext.Response.StatusCode = StatusCodes.Status303SeeOther;
        httpContext.Response.Headers.Location = loginUrl;
    }

    /// <summary>The login form posts <c>ReturnUrl</c>; the external-login start takes <c>returnUrl</c> in the query.</summary>
    private static async Task<string?> ReadReturnUrlAsync(
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync(cancellationToken);

            return form["ReturnUrl"].FirstOrDefault();
        }

        return request.Query["returnUrl"].FirstOrDefault();
    }
}
