using FluentValidation;
using Light.Mediator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Identity.Api;
using StarterKit.Infrastructure;
using StarterKit.Infrastructure.Caching;
using StarterKit.Infrastructure.Services;
using StarterKit.Shared;

namespace StarterKit.Identity.Web;

/// <summary>
/// Composition for the standalone Identity.Web host. The co-host (StarterKit.WebApi)
/// gets the equivalent platform + mediator services from its monolith-wide module scan
/// and <c>ConfigureExtensions</c>; this helper wires the same pipeline for the single
/// Identity assembly so a Razor page dispatching a mediator command gets the same
/// logging + validation behavior on both hosts.
/// </summary>
internal static class IdentityWebHost
{
    public static IServiceCollection AddIdentityWebHost(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddSharedInfrastructure();
        services.AddAppCache(configuration);
        services.AddScoped<ICurrentUser, ServerCurrentUser>();
        services.AddIdentityServices(configuration);
        services.AddIdentityWeb(configuration);

        // NOTE: this host does not call AddJwtTokenServices, so IAuthenticationService is not
        // registered here. The new Account/ExternalLoginRelay page (see Pages/Account) depends on
        // it, but that page's counterpart - the auth/token/external exchange endpoint on
        // Identity.Api's TokenController - is only mapped by the co-host (StarterKit.WebApi),
        // which does call AddJwtTokenServices via IdentityModule. The relay is therefore only
        // functional end-to-end under the co-host; wiring Jwt services here too would require a
        // "Jwt:SecretKey" in this host's own configuration for a flow it cannot complete alone,
        // and AddJwtTokenServices throws at startup if that section is missing - a worse outcome
        // for the standalone host's existing password/AD + interactive Microsoft login than
        // leaving this one page non-functional in that mode.

        // Mediator pipeline, scoped to the Identity assembly only. The standalone host
        // has NO cross-module notification handlers, so ExternalUserProvisionedIntegrationEvent
        // — and any future cross-module integration event — is intentionally left unhandled
        // here: the welcome email for a Microsoft-provisioned (JIT) user is sent only when
        // running under the co-host (StarterKit.WebApi), which scans every module assembly.
        // The standalone host is login-only, not a full notification pipeline.
        //
        // The behaviors below (logging + validation) now match the co-host. They are safe
        // with zero registered validators — ValidationBehaviour skips when none match.
        services.AddValidatorsFromAssemblies([typeof(IdentityModule).Assembly]);
        services.AddMediatorFromAssemblies(typeof(IdentityModule).Assembly);
        services.AddBehaviors(
            typeof(LoggingBehaviour<,>),
            typeof(ValidationBehaviour<,>)
            );

        return services;
    }
}
