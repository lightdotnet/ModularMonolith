using Light.AspNetCore.Authorization;

namespace StarterKit.LeaveManagement.Contracts.Authorization;

public class LeaveManagementPermissionProvider : IPermissionDefinitionProvider
{
    public IEnumerable<PermissionDefinition> Define()
    {
        yield return new(
            LeaveManagementPermissions.Requests.Manage,
            "Manage All Leave Requests",
            LeaveManagementPermissions.Group);
    }
}
