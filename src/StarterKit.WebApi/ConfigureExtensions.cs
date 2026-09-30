using Asp.Versioning.Conventions;
using FluentValidation;
using Light.AspNetCore.Builder;
using Light.AspNetCore.Middlewares;
using Light.AspNetCore.Swagger;
using Light.Mediator;
using Microsoft.AspNetCore.RateLimiting;
using StarterKit.EventBusMassTransitRabbitMQ;
using StarterKit.WebApi.Authentication;
using StarterKit.Infrastructure;
using StarterKit.Infrastructure.Caching;
using StarterKit.Infrastructure.Cors;
using StarterKit.Infrastructure.HealthChecks;
using StarterKit.Infrastructure.Modularity;
using StarterKit.Infrastructure.Services;
using StarterKit.Modules.Identity;
using StarterKit.Modules.Identity.Web;
using StarterKit.Shared;
using StarterKit.Shared.Authorization;
using System.Reflection;
using System.Threading.RateLimiting;

namespace StarterKit.WebApi;

public static class ConfigureExtensions
{
    private static readonly Assembly[] assemblies =
        [
            Assembly.GetExecutingAssembly(),
            typeof(IdentityModule).Assembly,
        ];

    public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatorsFromAssemblies(
            assemblies,
            includeInternalTypes: true);

        // Light Framework
        services.AddMediatorFromAssemblies(assemblies);
        services.AddBehaviors(
            typeof(LoggingBehaviour<,>),
            typeof(ValidationBehaviour<,>)
            );
        services.AddOptions<RequestLoggingOptions>().BindConfiguration("RequestLogging");
        services.AddGlobalExceptionHandler();
        services.AddApiVersion(1);
        services.AddSwagger(configuration);
        services.AddFileGenerator();

        services.AddSharedInfrastructure();
        services.AddAppCache(configuration);
        services.AddHealthChecksService();
        services.AddCorsPolicy(configuration);

        // IP-based fixed-window limiter guarding the external-login relay pages (Identity.Web)
        // and the auth/token/external exchange endpoint (TokenController) - all three are
        // anonymous, so they need their own throttle independent of the authenticated user.
        services.AddRateLimiter(options =>
        {
            options.AddPolicy("external-login", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));
        });

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, ServerCurrentUser>();
        services.AddPermissionPolicies();
        services.AddPermissionAuthorization();

        services.AddEventBus(
            configuration,
            assemblies);

        services.AddModules<AppModule>(
            configuration,
            assemblies);

        services.AddIdentityWeb(configuration);
        services.AddApiAuthentication(configuration);

        return services;
    }

    public static WebApplication ConfigurePipelines(this WebApplication app)
    {
        app
            .UseGuidV7TraceId()
            .UseLightExceptionHandler()
            .UseLightRequestLogging()
            .UseStaticFiles()
            .UseRouting()
            .UseCorsPolicy() // must add before Auth
            .UseRateLimiter()
            .UseAuthentication()
            .UseAuthorization()
            .UseSwagger();

        app.MapHealthChecksEndpoint();

        app.UseModules<AppModule>(assemblies);

        app.MapModuleEndpoints<AppModuleEndpoint>(assemblies);

        //register api versions
        var versions = app
            .NewApiVersionSet()
            .HasApiVersion(1)
            .ReportApiVersions()
            .Build();

        //map versioned endpoint
        var endpoints = app.MapGroup("api/v{version:apiVersion}").WithApiVersionSet(versions);
        endpoints.MapModuleEndpoints<AppModule>(assemblies);

        app.UseIdentityWeb();

        return app;
    }
}
