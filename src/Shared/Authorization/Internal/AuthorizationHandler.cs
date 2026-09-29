using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using StarterKit.Shared.Extensions;

namespace StarterKit.Shared.Authorization.Internal;

internal class AuthorizationHandler : PermissionAuthorizationHandler
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var user = context.User;

        var isFullControl = user.IsFullControl();

        var hasPermission = user.HasPermission(requirement.Permission);

        if (isFullControl || hasPermission)
        {
            context.Succeed(requirement);
        }

        await Task.CompletedTask;
    }
}