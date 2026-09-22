using Light.AspNetCore.Authorization;

namespace StarterKit.Transfers.Contracts.Authorization;

public class TransfersPermissionProvider : IPermissionDefinitionProvider
{
    public IEnumerable<PermissionDefinition> Define()
    {
        yield return new(
            TransfersPermissions.Transfers.View,
            "View Transfers",
            TransfersPermissions.Group);

        yield return new(
            TransfersPermissions.Transfers.Create,
            "Create Transfers",
            TransfersPermissions.Group);

        yield return new(
            TransfersPermissions.Transfers.Dispatch,
            "Dispatch Transfers",
            TransfersPermissions.Group);

        yield return new(
            TransfersPermissions.Transfers.Receive,
            "Receive Transfers",
            TransfersPermissions.Group);

        yield return new(
            TransfersPermissions.Transfers.Close,
            "Close Transfers",
            TransfersPermissions.Group);
    }
}
