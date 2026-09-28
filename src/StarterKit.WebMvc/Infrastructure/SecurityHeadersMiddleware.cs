using Microsoft.Extensions.Options;

namespace StarterKit.WebMvc.Infrastructure;

/// <summary>
/// Adds the browser hardening headers to every response: <c>X-Content-Type-Options</c>,
/// <c>Referrer-Policy</c>, <c>X-Frame-Options</c> and a Content-Security-Policy.
/// <para>
/// The CSP allows scripts/styles from this origin only — the views carry no inline scripts,
/// inline event handlers or <c>style</c> attributes (the flash-message JSON block is a
/// non-executing <c>application/json</c> data island, which CSP does not block), and Bootstrap's
/// JS only touches styles through the CSSOM, which <c>style-src</c> does not govern.
/// <c>connect-src</c> adds the SignalR hub origin (https + wss, or http + ws) and <c>form-action</c>
/// the Identity.Web origin used by the Microsoft external-login redirect. Development also allows
/// the <c>dotnet watch</c> browser-refresh socket on localhost.
/// </para>
/// Headers are applied in <see cref="HttpResponse.OnStarting(Func{Task})"/> so a later
/// <c>Response.Clear()</c> (e.g. by the API-authorization middleware) cannot drop them.
/// </summary>
internal sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    private readonly string _contentSecurityPolicy;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        IOptions<SignalROptions> signalROptions,
        IOptions<IdentityWebOptions> identityWebOptions,
        IHostEnvironment environment)
    {
        _next = next;
        _contentSecurityPolicy = BuildContentSecurityPolicy(
            signalROptions.Value.HubUrl,
            identityWebOptions.Value.BaseUrl,
            environment.IsDevelopment());
    }

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            headers.XContentTypeOptions = "nosniff";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers.XFrameOptions = "DENY";
            headers.ContentSecurityPolicy = _contentSecurityPolicy;

            return Task.CompletedTask;
        });

        return _next(context);
    }

    private static string BuildContentSecurityPolicy(
        string? hubUrl,
        string? identityWebUrl,
        bool isDevelopment)
    {
        var connectSources = new List<string> { "'self'" };

        if (TryGetOrigin(hubUrl, out var hub))
        {
            var secure = hub.Scheme == Uri.UriSchemeHttps;

            connectSources.Add(hub.GetLeftPart(UriPartial.Authority));
            connectSources.Add($"{(secure ? "wss" : "ws")}://{hub.Authority}");
        }

        if (isDevelopment)
        {
            connectSources.Add("ws://localhost:*");
            connectSources.Add("wss://localhost:*");
        }

        var formActions = new List<string> { "'self'" };

        if (TryGetOrigin(identityWebUrl, out var identityWeb))
        {
            formActions.Add(identityWeb.GetLeftPart(UriPartial.Authority));
        }

        return string.Join(
            "; ",
            "default-src 'self'",
            "base-uri 'self'",
            "object-src 'none'",
            "frame-ancestors 'none'",
            "script-src 'self'",
            "style-src 'self'",
            "img-src 'self' data:",
            "font-src 'self'",
            $"connect-src {string.Join(' ', connectSources.Distinct(StringComparer.OrdinalIgnoreCase))}",
            $"form-action {string.Join(' ', formActions.Distinct(StringComparer.OrdinalIgnoreCase))}");
    }

    private static bool TryGetOrigin(
        string? url,
        out Uri uri)
    {
        return Uri.TryCreate(
                url,
                UriKind.Absolute,
                out uri!)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }
}
