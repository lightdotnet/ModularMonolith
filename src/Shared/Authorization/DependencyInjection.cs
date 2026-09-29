using Light.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Shared.Authorization.Internal;

namespace StarterKit.Shared.Authorization;

public static class DependencyInjection
{
    public static IServiceCollection AddPermissionPolicies(this IServiceCollection services) =>
        services.AddPermissionPolicyProvider<PolicyProvider>();

    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services) =>
        services.AddPermissionAuthorizationHandler<AuthorizationHandler>();
}