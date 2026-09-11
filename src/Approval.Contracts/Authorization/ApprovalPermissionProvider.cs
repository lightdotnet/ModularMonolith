using Light.AspNetCore.Authorization;

namespace StarterKit.Approval.Contracts.Authorization;

public class ApprovalPermissionProvider : IPermissionDefinitionProvider
{
    public IEnumerable<PermissionDefinition> Define()
    {
        yield return new(
            ApprovalPermissions.Requests.ViewAll,
            "View All Approval Requests",
            ApprovalPermissions.Group);

        yield return new(
            ApprovalPermissions.DocumentTypes.View,
            "View Approval Document Types",
            ApprovalPermissions.Group);

        yield return new(
            ApprovalPermissions.DocumentTypes.Create,
            "Create Approval Document Types",
            ApprovalPermissions.Group);

        yield return new(
            ApprovalPermissions.DocumentTypes.Update,
            "Update Approval Document Types",
            ApprovalPermissions.Group);

        yield return new(
            ApprovalPermissions.DocumentTypes.Delete,
            "Delete Approval Document Types",
            ApprovalPermissions.Group);
    }
}
