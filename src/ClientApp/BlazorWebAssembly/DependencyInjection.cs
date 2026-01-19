using Blazored.LocalStorage;
using Light.AspNetCore.Authorization;
using Light.Blazor;
using Light.Extensions.DependencyInjection;
using Light.MudBlazor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Monolith.Authorization;
using Monolith.Blazor.Infrastructure;
using Monolith.Blazor.Infrastructure.Services;
using Monolith.Blazor.Services;
using Monolith.Blazor.Shared;
using Monolith.HttpApi;
using Monolith.HttpApi.Common.HttpFactory;
using Monolith.HttpApi.Common.Interfaces;

namespace Monolith.Blazor;

public static class DependencyInjection
{
    public static IServiceCollection AddBlazorComponents(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddBlazoredLocalStorageAsSingleton();
        services.AddSingleton<IStorageService, StorageService>();

        services.AddFileGenerator();
        services.AddMudBlazorExtraComponents();

        services.AddHttpClients(configuration);
        services.AddHttpClientServices(typeof(HttpApiClientModule).Assembly);

        services.AddSingleton<TokenStorage, TokenStorageService>();

        services.AddScoped<LayoutService>();
        services.AddScoped<SignalRClient>();

        services.AddAuth();

        return services;
    }

    public static IServiceCollection AddAuth(this IServiceCollection services)
    {
        // register authorization policies for all permissions
        services.AddAuthorizationCore(RegisterPermissions);
        services.AddCascadingAuthenticationState();

        // register the custom state provider
        services.AddSingleton<AuthenticationStateProvider, JwtAuthenticationStateProvider>();
        services.AddScoped(sp => (ISignInManager)sp.GetRequiredService<AuthenticationStateProvider>());
        services.AddScoped(sp => (ITokenProvider)sp.GetRequiredService<AuthenticationStateProvider>());
        services.AddScoped<IClientCurrentUser, ClientCurrentUser>();
        services.AddPermissionAuthorization();

        return services;
    }

    private static void RegisterPermissions(AuthorizationOptions options)
    {
        var permissions = ClientClaimsExtensions.GetAll().Claims.Select(s => s.Value).ToList();

        foreach (var permission in permissions)
        {
            options.AddPolicy(permission, policy =>
                policy.AddRequirements(new PermissionRequirement(permission)));
        }
    }
}