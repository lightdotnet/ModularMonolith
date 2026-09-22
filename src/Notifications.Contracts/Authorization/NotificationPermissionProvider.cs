using Light.AspNetCore.Authorization;

namespace StarterKit.Notifications.Contracts.Authorization;

public class NotificationPermissionProvider : IPermissionDefinitionProvider
{
    public IEnumerable<PermissionDefinition> Define()
    {
        yield return new(
            NotificationPermissions.Read,
            "Read Notifications",
            NotificationPermissions.Group);

        yield return new(
            NotificationPermissions.Send,
            "Send Notifications",
            NotificationPermissions.Group);
    }
}
