using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StarterKit.WebMvc.Authentication;
using StarterKit.WebMvc.Authorization;
using StarterKit.WebMvc.Services.Http;
using StarterKit.WebMvc.Services.Identity;
using StarterKit.WebMvc.Services.Notifications;

namespace StarterKit.WebMvc.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Antiforgery header read on fetch requests; <c>site.js</c> sends the token under this name.</summary>
    public const string AntiforgeryHeaderName = "RequestVerificationToken";

    /// <summary>
    /// MVC + Razor Pages, antiforgery, configuration options, forwarded-headers trust, sign-in rate
    /// limiting and the typed backend module clients.
    /// </summary>
    public static IServiceCollection AddWebMvcServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ApiOptions>()
            .Bind(configuration.GetSection(ApiOptions.SectionName))
            .Validate(
                options => IsAbsoluteUrl(options.Identity.BaseUrl) && IsAbsoluteUrl(options.Notifications.BaseUrl),
                "Api:Identity:BaseUrl and Api:Notifications:BaseUrl must be absolute URLs.")
            .ValidateOnStart();

        services.AddOptions<IdentityWebOptions>()
            .Bind(configuration.GetSection(IdentityWebOptions.SectionName));

        services.AddOptions<SignalROptions>()
            .Bind(configuration.GetSection(SignalROptions.SectionName));

        services.AddOptions<ExternalLoginOptions>()
            .Bind(configuration.GetSection(ExternalLoginOptions.SectionName));

        services.AddOptions<PermissionOptions>()
            .Bind(configuration.GetSection(PermissionOptions.SectionName));

        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();

        services.AddControllersWithViews(options =>
        {
            options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
        });

        services.AddRazorPages();

        services.AddAntiforgery(options =>
        {
            options.HeaderName = AntiforgeryHeaderName;
        });

        services.AddForwardedHeadersTrust(configuration);
        services.AddSignInRateLimiting(configuration);
        services.AddBackendClients();

        return services;
    }

    /// <summary>
    /// Cookie session (sign-in/refresh), Data Protection, and permission-based authorization with
    /// an authenticated-user fallback policy.
    /// </summary>
    public static IServiceCollection AddWebMvcAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddWebMvcDataProtection(
            configuration,
            environment);

        services.AddScoped<SessionCookieEvents>();
        services.AddScoped<ISessionSignInService, SessionSignInService>();
        services.AddSingleton<ExternalLoginPkce>();

        services.AddAuthentication(SessionDefaults.AuthenticationScheme)
            .AddCookie(
                SessionDefaults.AuthenticationScheme,
                options =>
                {
                    options.Cookie.Name = SessionDefaults.CookieName;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.IsEssential = true;

                    // Development runs over plain http too; everywhere else the cookie is Secure.
                    options.Cookie.SecurePolicy = environment.IsDevelopment()
                        ? CookieSecurePolicy.SameAsRequest
                        : CookieSecurePolicy.Always;

                    options.LoginPath = SessionDefaults.LoginPath;
                    options.LogoutPath = SessionDefaults.LogoutPath;
                    options.AccessDeniedPath = SessionDefaults.AccessDeniedPath;

                    // Hard cap from sign-in; SessionTicket pins the expiry across refreshes.
                    options.ExpireTimeSpan = SessionDefaults.SessionLifetime;
                    options.SlidingExpiration = false;

                    options.EventsType = typeof(SessionCookieEvents);
                });

        services.AddSingleton<IPermissionChecker, PermissionChecker>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder(SessionDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build());

        return services;
    }

    /// <summary>Adds the hardening headers (<see cref="SecurityHeadersMiddleware"/>). Place right after <c>UseForwardedHeaders</c>.</summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }

    /// <summary>
    /// Maps backend 401/403 responses (<see cref="ApiAuthorizationException"/>) to sign-out/login
    /// and access-denied. Place after <c>UseAuthorization</c>.
    /// </summary>
    public static IApplicationBuilder UseApiAuthorizationFailureHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiAuthorizationExceptionMiddleware>();
    }

    /// <summary>
    /// Data Protection encrypts the session/antiforgery/TempData/PKCE cookies. Development keeps the
    /// default per-user key store. Every other environment MUST configure
    /// <c>DataProtection:KeysPath</c> (a persistent directory shared by all instances) and SHOULD
    /// configure a certificate to encrypt the keys at rest — see <see cref="DataProtectionKeyOptions"/>.
    /// </summary>
    private static void AddWebMvcDataProtection(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var dataProtection = services.AddDataProtection()
            .SetApplicationName("StarterKit.WebMvc");

        if (environment.IsDevelopment())
        {
            return;
        }

        var options = configuration.GetSection(DataProtectionKeyOptions.SectionName).Get<DataProtectionKeyOptions>()
            ?? new DataProtectionKeyOptions();

        if (!string.IsNullOrWhiteSpace(options.KeysPath))
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(options.KeysPath));
        }

        if (!string.IsNullOrWhiteSpace(options.CertificateThumbprint))
        {
            dataProtection.ProtectKeysWithCertificate(options.CertificateThumbprint);
        }
        else if (!string.IsNullOrWhiteSpace(options.CertificatePath))
        {
            dataProtection.ProtectKeysWithCertificate(X509CertificateLoader.LoadPkcs12FromFile(
                options.CertificatePath,
                options.CertificatePassword));
        }
    }

    /// <summary>
    /// Honours <c>X-Forwarded-For</c>/<c>-Proto</c> from the configured proxies only (loopback is
    /// trusted by default) — see <see cref="ForwardedHeadersSettings"/>.
    /// </summary>
    private static void AddForwardedHeadersTrust(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection(ForwardedHeadersSettings.SectionName).Get<ForwardedHeadersSettings>()
            ?? new ForwardedHeadersSettings();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            foreach (var proxy in settings.KnownProxies.Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy.Trim()));
            }

            foreach (var network in settings.KnownNetworks.Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network.Trim()));
            }
        });
    }

    private static void AddBackendClients(this IServiceCollection services)
    {
        services.AddTransient<BearerTokenHandler>();

        services.AddModuleHttpClient(
            ApiClientNames.Identity,
            options => options.Identity.BaseUrl);

        services.AddModuleHttpClient(
            ApiClientNames.Notifications,
            options => options.Notifications.BaseUrl);

        services.AddScoped<IAuthClient, AuthClient>();
        services.AddScoped<IUserClient, UserClient>();
        services.AddScoped<IRoleClient, RoleClient>();
        services.AddScoped<IPermissionClient, PermissionClient>();
        services.AddScoped<IUserProfileClient, UserProfileClient>();

        services.AddScoped<INotificationAdminClient, NotificationAdminClient>();
        services.AddScoped<IMyNotificationClient, MyNotificationClient>();
    }

    private static void AddModuleHttpClient(
        this IServiceCollection services,
        string name,
        Func<ApiOptions, string> baseUrl)
    {
        services
            .AddHttpClient(
                name,
                (serviceProvider, client) =>
                {
                    var url = baseUrl(serviceProvider.GetRequiredService<IOptions<ApiOptions>>().Value);

                    // A trailing slash keeps the configured path prefix (e.g. api/v1/) when
                    // relative request paths are resolved against the base address.
                    client.BaseAddress = new Uri(url.EndsWith('/') ? url : $"{url}/");
                })
            .AddHttpMessageHandler<BearerTokenHandler>();
    }

    private static bool IsAbsoluteUrl(string? value) =>
        Uri.TryCreate(
            value,
            UriKind.Absolute,
            out _);
}
