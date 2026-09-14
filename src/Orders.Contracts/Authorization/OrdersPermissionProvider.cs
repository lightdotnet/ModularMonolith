using Light.AspNetCore.Authorization;

namespace StarterKit.Orders.Contracts.Authorization;

public class OrdersPermissionProvider : IPermissionDefinitionProvider
{
    public IEnumerable<PermissionDefinition> Define()
    {
        yield return new(
            OrdersPermissions.Orders.View,
            "View Orders",
            OrdersPermissions.Group);

        yield return new(
            OrdersPermissions.Orders.Manage,
            "Manage Orders",
            OrdersPermissions.Group);

        yield return new(
            OrdersPermissions.Payments.View,
            "View Payments",
            OrdersPermissions.Group);

        yield return new(
            OrdersPermissions.Payments.Manage,
            "Manage Payments",
            OrdersPermissions.Group);
    }
}
