using System.Security.Claims;
using StarterKit.Inventory.Contracts.Authorization;
using StarterKit.Purchasing.Contracts.Authorization;
using StarterKit.Shared.Authorization;
using StarterKit.Shared.Extensions;

namespace StarterKit.Purchasing.Api.Controllers;

/// <summary>
/// Caller-capability helpers for handlers that shape their behaviour on more than the endpoint permission.
/// Both honour full control like <c>[MustHavePermission]</c> does.
/// </summary>
internal static class PurchasingAccess
{
    /// <summary>
    /// A purchasing manager (holder of <see cref="PurchasingPermissions.Orders.Close"/>) may edit or cancel
    /// purchase orders requested by someone else, and cancel an approved order.
    /// </summary>
    public static bool CanManageOrders(this ClaimsPrincipal user) =>
        user.HasPermission(PurchasingPermissions.Orders.Close) || user.IsFullControl();

    /// <summary>
    /// The cost Inventory removed from stock is its moving-average cost, so it is gated by Inventory's own
    /// <c>view_cost</c> permission. Purchasing's own purchase prices are not masked.
    /// </summary>
    public static bool CanViewStockCost(this ClaimsPrincipal user) =>
        user.HasPermission(InventoryPermissions.Stock.ViewCost) || user.IsFullControl();
}
