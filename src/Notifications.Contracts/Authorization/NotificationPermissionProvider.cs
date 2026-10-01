using Light.AspNetCore.Authorization;

namespace StarterKit.Modules.Notifications.Contracts.Authorization;

public sealed class NotificationPermissionProvider : IPermissionDefinitionProvider
{
    public IEnumerable<PermissionDefinition> Define()
    {
        yield return new(NotificationPermissions.Read, "Read Notifications", NotificationPermissions.Group);

        yield return new(NotificationPermissions.Send, "Send Notifications", NotificationPermissions.Group);
    }
}
