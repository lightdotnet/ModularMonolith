using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using StarterKit.Modules.Identity.Domain;
using StarterKit.Modules.Identity.Web.Admin;
using StarterKit.Modules.Identity.Web.Authentication;
using StarterKit.Modules.Identity.Web.TagHelpers;

namespace StarterKit.Modules.Identity.Web;

public static class DependencyInjection
{
    private const string CorrelationCookieName = "Identity.Microsoft.Correlation";

    private const string MicrosoftSignInFailedMessage =
        "Microsoft sign-in could not be completed. Start again and use a fresh browser session.";

    internal const string SecurityStampValidationIntervalKey = "IdentityWeb:SecurityStampValidationInterval";

    internal static readonly TimeSpan DefaultSecurityStampValidationInterval = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Microsoft (Entra ID) sign-in is optional — it is wired up only when a client id, a
    /// client secret, and at least one allow-listed tenant id are all configured. Without
    /// them the login page shows the password form only, and the app still starts.
    /// </summary>
    public static bool IsMicrosoftLoginEnabled(this IConfiguration configuration) =>
        configuration.GetSection("Authentication:Microsoft").Get<MicrosoftOidcOptions>()?.IsEnabled ?? false;

    /// <summary>
    /// Registers the web/auth concerns only — cookie + external-provider schemes, the
    /// <see cref="SignInManager{TUser}"/>, authorization, and Razor Pages. The Identity
    /// domain services and shared infrastructure it depends on are composed by each host
    /// (the co-host via its module scan, the standalone host in <c>Program.cs</c>).
    /// </summary>
    public static IServiceCollection AddIdentityWeb(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // One scoped instance serves both the concrete type and SignInManager<User>, so every
        // consumer (pages, SecurityStampValidator<User>) gets the status-aware sign-in checks.
        services.AddScoped<IdentitySignInManager>();
        services.AddScoped<SignInManager<User>>(sp => sp.GetRequiredService<IdentitySignInManager>());

        services.AddOptions<ExternalLoginRelayOptions>().BindConfiguration("ExternalLoginRelay");

        var microsoft = configuration
            .GetSection("Authentication:Microsoft")
            .Get<MicrosoftOidcOptions>()
            ?? new();

        var authenticationBuilder = services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
            })
            .AddCookie(IdentityConstants.ApplicationScheme, options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";

                // Re-validate the cookie against the user's security stamp, so a session whose
                // roles/permissions changed (the stamp is rotated) is dropped and an unchanged one
                // gets its claims refreshed at every validation interval.
                options.Events.OnValidatePrincipal = SecurityStampValidator.ValidatePrincipalAsync;
            })
            .AddCookie(IdentityConstants.ExternalScheme);

        services.TryAddScoped<ISecurityStampValidator, SecurityStampValidator<User>>();

        // How often the cookie is re-checked against the security stamp. Shorter means a locked
        // user or a role/permission change takes effect sooner, at the cost of one user load and
        // claims rebuild per session per interval. Only cookie-authenticated Razor Pages pay it;
        // /api is Bearer-authenticated and never runs this validator.
        var securityStampValidationInterval = configuration.GetValue<TimeSpan?>(SecurityStampValidationIntervalKey)
            ?? DefaultSecurityStampValidationInterval;

        services
            .AddOptions<SecurityStampValidatorOptions>()
            .Configure(options => options.ValidationInterval = securityStampValidationInterval)
            .Validate(
                options => options.ValidationInterval > TimeSpan.Zero,
                $"{SecurityStampValidationIntervalKey} must be a positive TimeSpan.")
            .ValidateOnStart();

        if (microsoft.IsEnabled)
            authenticationBuilder.AddMicrosoftOpenIdConnect(microsoft);

        services.AddAuthorization();

        services.AddIdentityAdmin();

        services
            .AddRazorPages()
            .AddApplicationPart(typeof(DependencyInjection).Assembly);

        return services;
    }

    /// <summary>
    /// Admin page toggles (<see cref="IdentityAdminOptions"/>), the route convention that removes
    /// disabled admin pages, and the <see cref="IFeatureToggle"/> behind <c>asp-feature</c>.
    /// </summary>
    private static IServiceCollection AddIdentityAdmin(this IServiceCollection services)
    {
        services
            .AddOptions<IdentityAdminOptions>()
            .BindConfiguration(IdentityAdminOptions.SectionName);

        services
            .AddOptions<RazorPagesOptions>()
            .Configure<IOptions<IdentityAdminOptions>>((options, admin) =>
                options.Conventions.Add(new AdminPagesConvention(admin.Value)));

        services.TryAddSingleton<IFeatureToggle, AdminFeatureToggle>();

        return services;
    }

    private static AuthenticationBuilder AddMicrosoftOpenIdConnect(
        this AuthenticationBuilder builder,
        MicrosoftOidcOptions microsoft)
    {
        var instance = microsoft.Instance;
        var clientId = microsoft.ClientId!;
        var clientSecret = microsoft.ClientSecret!;
        var allowedTenantIds = microsoft.AllowedTenantIds;

        return builder
            .AddOpenIdConnect("Microsoft", options =>
            {
                options.Authority = $"{instance}organizations/v2.0";
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
                options.CallbackPath = microsoft.CallbackPath;
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.CorrelationCookie = new CookieBuilder
                {
                    Name = CorrelationCookieName,
                    HttpOnly = true,
                    IsEssential = true,
                    SameSite = SameSiteMode.None,
                    SecurePolicy = CookieSecurePolicy.Always
                };
                options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;

                options.AccessDeniedPath = "/Account/Login";

                options.ResponseType = "code";
                options.UsePkce = true;
                options.SaveTokens = true;

                options.Scope.Add("email");
                options.GetClaimsFromUserInfoEndpoint = true;

                // Keep raw claim types (tid / oid / preferred_username) instead of the
                // legacy SOAP-style mapping — the descriptor mapper depends on the raw names.
                options.MapInboundClaims = false;

                options.TokenValidationParameters.ValidateIssuer = true;
                options.TokenValidationParameters.IssuerValidator = (issuer, token, _) =>
                {
                    var tid = token is JsonWebToken jwt
                        && jwt.TryGetPayloadValue<string>("tid", out var value)
                        ? value
                        : null;

                    if (string.IsNullOrEmpty(tid)
                        || !allowedTenantIds.Contains(tid, StringComparer.OrdinalIgnoreCase))
                    {
                        throw new SecurityTokenInvalidIssuerException("The token tenant is not allow-listed.");
                    }

                    var expected = $"{instance}{tid}/v2.0";
                    if (!string.Equals(issuer, expected, StringComparison.OrdinalIgnoreCase))
                        throw new SecurityTokenInvalidIssuerException("The token issuer does not match the expected tenant issuer.");

                    return issuer;
                };

                options.TokenValidationParameters.ValidateAudience = true;
                options.TokenValidationParameters.ValidAudience = clientId;

                options.Events.OnTokenValidated = context =>
                {
                    var tid = context.Principal?.FindFirst("tid")?.Value;
                    if (tid is null || !allowedTenantIds.Contains(tid, StringComparer.OrdinalIgnoreCase))
                        context.Fail("Tenant not allowed.");

                    return Task.CompletedTask;
                };

                options.Events.OnRedirectToIdentityProvider = context =>
                {
                    context.ProtocolMessage.Prompt = "select_account";
                    return Task.CompletedTask;
                };

                options.Events.OnRemoteFailure = context =>
                {
                    context.HttpContext
                        .RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("StarterKit.Modules.Identity.Web.MicrosoftOidc")
                        .LogWarning(
                            context.Failure,
                            "Microsoft OIDC remote failure: {Message}",
                            context.Failure?.Message);

                    context.HandleResponse();

                    if (!context.Response.HasStarted)
                        context.Response.Redirect(QueryHelpers.AddQueryString(
                            "/Account/Login",
                            "Error",
                            MicrosoftSignInFailedMessage));

                    return Task.CompletedTask;
                };
            });
    }

    public static WebApplication UseIdentityWeb(this WebApplication app)
    {
        app.MapRazorPages();
        return app;
    }
}
