using Light.AspNetCore.Authorization;

namespace StarterKit.Inventory.Contracts.Authorization;

public class InventoryPermissionProvider : IPermissionDefinitionProvider
{
    public IEnumerable<PermissionDefinition> Define()
    {
        yield return new(
            InventoryPermissions.Stock.View,
            "View Stock",
            InventoryPermissions.Group);

        yield return new(
            InventoryPermissions.Stock.Manage,
            "Manage Stock",
            InventoryPermissions.Group);

        yield return new(
            InventoryPermissions.Stock.ViewCost,
            "View Stock Cost",
            InventoryPermissions.Group);

        yield return new(
            InventoryPermissions.Stock.Revalue,
            "Revalue Stock Cost",
            InventoryPermissions.Group);
    }
}
