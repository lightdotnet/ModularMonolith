using Light.AspNetCore.Authorization;

namespace StarterKit.Modules.Identity.Contracts.Authorization;

public sealed class IdentityPermissionProvider : IPermissionDefinitionProvider
{
    public IEnumerable<PermissionDefinition> Define()
    {
        yield return new(IdentityPermissions.Users.View, "View Users", "users");

        yield return new(IdentityPermissions.Users.Create, "Create Users", "users");

        yield return new(IdentityPermissions.Users.Update, "Update Users", "users");

        yield return new(IdentityPermissions.Users.Delete, "Delete Users", "users");

        yield return new(IdentityPermissions.Roles.View, "View Roles", "roles");

        yield return new(IdentityPermissions.Roles.Manage, "Manage Roles", "roles");
    }
}
