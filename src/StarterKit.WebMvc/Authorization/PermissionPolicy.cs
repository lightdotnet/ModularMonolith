using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using StarterKit.WebMvc.Authentication;

namespace StarterKit.WebMvc.Authorization;

/// <summary>
/// Requires the named permission (see <see cref="IPermissionChecker"/>).
/// </summary>
public sealed record PermissionRequirement(
    string Permission)
    : IAuthorizationRequirement;

/// <summary>
/// Resolves <c>perm:&lt;permission&gt;</c> policy names on the fly, so every permission constant
/// (e.g. <c>IdentityPermissions.Users.View</c>) is usable as a policy without registering each
/// one; any other name falls through to the statically registered policies.
/// </summary>
internal sealed class PermissionPolicyProvider(
    IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public const string PolicyPrefix = "perm:";

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(PolicyPrefix, StringComparison.Ordinal)
            || policyName.Length == PolicyPrefix.Length)
        {
            return await base.GetPolicyAsync(policyName);
        }

        return new AuthorizationPolicyBuilder(SessionDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(policyName[PolicyPrefix.Length..]))
            .Build();
    }
}

internal sealed class PermissionAuthorizationHandler(
    IPermissionChecker permissionChecker)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (permissionChecker.HasPermission(
            context.User,
            requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Requires a backend permission on a controller, action or PageModel, e.g.
/// <c>[HasPermission(IdentityPermissions.Users.View)]</c>. Unauthenticated users are sent to the
/// login page, authenticated users lacking the permission to the access-denied page.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
    {
        Permission = permission;
        Policy = PermissionPolicyProvider.PolicyPrefix + permission;
    }

    public string Permission { get; }
}
