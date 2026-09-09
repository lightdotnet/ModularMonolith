using Light.Exceptions;
using Light.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using StarterKit.Identity.Api.Jwt;
using StarterKit.Notifications.Api.SignalR;
using StarterKit.Notifications.Contracts.SystemNotifications;
using StarterKit.Shared.Constants;
using System.Text;

namespace StarterKit.WebApi.Authentication;

/// <summary>
/// Co-host only. Adds the Bearer scheme and the policy scheme that lets the single
/// StarterKit.WebApi process serve both the JSON API (Bearer / SignalR hub) and the
/// Identity Razor Pages (application cookie) behind one authentication pipeline.
/// The standalone Identity.Web host never calls this.
/// </summary>
public static class ApiAuthenticationExtensions
{
    private const string CookieOrBearerScheme = "Identity.CookieOrBearer";

    // Dedicated JwtBearer sub-scheme for the SignalR hub handshake token. Unlike the
    // vendor "Bearer" scheme it validates the audience, so a token that is not stamped
    // with the hub audience fails authentication outright rather than relying on a
    // downstream authorization handler to notice the claim.
    private const string HubBearerScheme = "HubBearer";

    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>();
        if (jwtOptions is null || string.IsNullOrWhiteSpace(jwtOptions.SecretKey))
            throw new InvalidOperationException("Configuration section 'Jwt' is missing or invalid.");

        // Resolve the SignalR hub path once so the scheme selector and the cookie redirect
        // events route by request path rather than by the Authorization header. This reads the
        // raw section directly and runs before the options ValidateOnStart hook, so it needs
        // its own guard: a bad value here would silently break scheme routing for every request.
        var hubPath = configuration.GetSection(NotificationHubOptions.SectionName)
            .Get<NotificationHubOptions>()?.Path ?? new NotificationHubOptions().Path;
        if (!NotificationHubOptionsSetup.IsValidHubPath(hubPath))
            throw new InvalidOperationException(NotificationHubOptionsSetup.InvalidPathMessage);

        // Register + validate the options for DI (consumed by HubTokenApiGuardHandler); shared
        // with the Notifications module so both bind and validate one identical value.
        services.AddNotificationHubOptions(configuration);

        // The vendor AddJwtAuth is a black box: it registers the "Bearer" scheme (plus the
        // SignalR hub ?access_token= query pickup) and grabs the app-global default schemes
        // for itself. We immediately re-open AddAuthentication below and point the defaults at
        // our policy scheme, so the resolved default is deterministic within this one method
        // rather than dependent on which registration ran last.
        // The 4th arg wires the vendor "Bearer" scheme's own ?access_token= pickup for the hub
        // path. In this co-host that path is unreachable: the policy scheme's selector forwards
        // every hub-path request to HubBearer, never to "Bearer". It stays as belt-and-suspenders
        // in case the selector ever regresses -- HubBearer owns the hub handshake today.
        services.AddJwtAuth(
            jwtOptions.Issuer,
            jwtOptions.SecretKey,
            ClaimTypeConstants.Role,
            hubPath);

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieOrBearerScheme;
                options.DefaultAuthenticateScheme = CookieOrBearerScheme;
                options.DefaultChallengeScheme = CookieOrBearerScheme;
            })
            // The Notifications SignalR hub is bearer-protected but reached over a browser
            // WebSocket, which can't send an Authorization header — its requests need special
            // handling in both the scheme selector and the cookie redirect events.
            .AddPolicyScheme(CookieOrBearerScheme, "Cookie or Bearer authentication", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var path = context.Request.Path;

                    // The Notifications SignalR hub is always reached with the dedicated hub-audience
                    // handshake token — in the Authorization header on the negotiate POST, and as
                    // ?access_token= on the WebSocket upgrade (browsers can't set WS headers). Both must
                    // authenticate on the fail-closed HubBearer scheme; the main Bearer scheme is
                    // post-configured to reject the hub audience outright.
                    if (path.StartsWithSegments(hubPath))
                        return HubBearerScheme;

                    // The JSON API is always Bearer. Routing it here rather than letting a header-less
                    // request fall through to the cookie scheme keeps every 401/403 flowing through the
                    // vendor JwtBearer OnChallenge/OnForbidden -> UnauthorizedException/ForbiddenException,
                    // which UseLightExceptionHandler renders as the standard error envelope.
                    if (path.StartsWithSegments("/api"))
                        return JwtBearerDefaults.AuthenticationScheme;

                    // Everything else (Identity.Web Razor Pages, /signin-oidc) is cookie-authenticated.
                    return IdentityConstants.ApplicationScheme;
                };
            })
            .AddJwtBearer(HubBearerScheme, ConfigureHubBearerScheme(jwtOptions, hubPath));

        // Fail-closed audience isolation for the main API scheme. The vendor "Bearer" scheme
        // sets ValidateAudience = false, so a hub token replayed against /api authenticates
        // and only the HubTokenApiGuardHandler stops it. Post-configure the scheme to reject
        // any token carrying the hub audience while still accepting the audience-less full API
        // token, and flow the validated token's expiry into the auth ticket so SignalR's
        // CloseOnAuthenticationExpiration can drop a hub connection when its 120s token lapses.
        services.PostConfigure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            options =>
            {
                // Allow the audience-less full API token (no aud claim) and reject any
                // token that carries the hub audience.
                var hubAudience = jwtOptions.HubAudience;
                options.TokenValidationParameters.ValidateAudience = true;
                options.TokenValidationParameters.AudienceValidator = (audiences, _, _) =>
                    audiences is null ||
                    !audiences.Any(audience => string.Equals(audience, hubAudience, StringComparison.Ordinal));

                options.Events ??= new JwtBearerEvents();
                var priorOnTokenValidated = options.Events.OnTokenValidated;
                options.Events.OnTokenValidated = async context =>
                {
                    await priorOnTokenValidated(context);
                    ApplyTokenExpiryToTicket(context);
                };
            });

        // Defense in depth. With the HubBearer sub-scheme above as the primary (fail-closed)
        // control, this handler is redundant for the /api direction but stays as a cheap
        // backstop in case audience validation is ever weakened.
        services.AddScoped<IAuthorizationHandler, HubTokenApiGuardHandler>();

        services.PostConfigure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, options =>
        {
            options.Events.OnRedirectToLogin = context =>
                ApiRequestOrRedirect(context, StatusCodes.Status401Unauthorized, hubPath);
            options.Events.OnRedirectToAccessDenied = context =>
                ApiRequestOrRedirect(context, StatusCodes.Status403Forbidden, hubPath);
        });

        return services;
    }

    // Same issuer and signing key as the vendor "Bearer" scheme, but audience validation is
    // on and pinned to the hub audience, and the token is pulled from ?access_token= for the
    // hub path the same way the vendor scheme does for its own bearer flow.
    private static Action<JwtBearerOptions> ConfigureHubBearerScheme(
        JwtOptions jwtOptions,
        string hubPath) =>
        options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.HubAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                RoleClaimType = ClaimTypeConstants.Role,
            };
            options.Events = new JwtBearerEvents
            {
                // Mirror the vendor "Bearer" scheme: turn a hub-token authentication failure
                // (expired, tampered, wrong audience) into an UnauthorizedException so it flows
                // through UseLightExceptionHandler and returns the standard error envelope
                // instead of a bare 401. No OnForbidden -- the hub has no permission checks.
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    if (!context.Response.HasStarted)
                        throw new UnauthorizedException("Authentication Failed.");

                    return Task.CompletedTask;
                },
                OnMessageReceived = context =>
                {
                    if (string.IsNullOrEmpty(context.Token) &&
                        context.HttpContext.Request.Path.StartsWithSegments(hubPath))
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken))
                            context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    ApplyTokenExpiryToTicket(context);
                    return Task.CompletedTask;
                },
            };
        };

    // The JwtBearer handler does not copy the validated token's expiry into the auth ticket
    // on its own; SignalR's CloseOnAuthenticationExpiration needs Properties.ExpiresUtc set to
    // tear a live connection down when the token lapses.
    private static void ApplyTokenExpiryToTicket(TokenValidatedContext context)
    {
        if (context.Properties is not null &&
            context.SecurityToken is JsonWebToken jwt &&
            jwt.ValidTo != DateTime.MinValue)
        {
            context.Properties.ExpiresUtc = jwt.ValidTo;
        }
    }

    private static Task ApiRequestOrRedirect(
        RedirectContext<CookieAuthenticationOptions> context,
        int statusCode,
        string hubPath)
    {
        if (context.Request.Path.StartsWithSegments("/api")
            || context.Request.Path.StartsWithSegments(hubPath))
        {
            context.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }
}
