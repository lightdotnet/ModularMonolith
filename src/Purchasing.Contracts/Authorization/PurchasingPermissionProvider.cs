using Light.AspNetCore.Authorization;

namespace StarterKit.Purchasing.Contracts.Authorization;

public class PurchasingPermissionProvider : IPermissionDefinitionProvider
{
    public IEnumerable<PermissionDefinition> Define()
    {
        yield return new(
            PurchasingPermissions.Suppliers.View,
            "View Suppliers",
            PurchasingPermissions.Group);

        yield return new(
            PurchasingPermissions.Suppliers.Manage,
            "Manage Suppliers",
            PurchasingPermissions.Group);

        yield return new(
            PurchasingPermissions.Orders.View,
            "View Purchase Orders",
            PurchasingPermissions.Group);

        yield return new(
            PurchasingPermissions.Orders.Create,
            "Create Purchase Orders",
            PurchasingPermissions.Group);

        yield return new(
            PurchasingPermissions.Orders.Submit,
            "Submit Purchase Orders",
            PurchasingPermissions.Group);

        yield return new(
            PurchasingPermissions.Orders.Close,
            "Close Purchase Orders",
            PurchasingPermissions.Group);

        yield return new(
            PurchasingPermissions.Receipts.View,
            "View Goods Receipts",
            PurchasingPermissions.Group);

        yield return new(
            PurchasingPermissions.Receipts.Create,
            "Create Goods Receipts",
            PurchasingPermissions.Group);

        yield return new(
            PurchasingPermissions.Returns.View,
            "View Purchase Returns",
            PurchasingPermissions.Group);

        yield return new(
            PurchasingPermissions.Returns.Create,
            "Create Purchase Returns",
            PurchasingPermissions.Group);

        yield return new(
            PurchasingPermissions.Returns.Credit,
            "Credit Purchase Returns",
            PurchasingPermissions.Group);
    }
}
