using System.Security.Claims;
using StarterKit.Inventory.Contracts.Authorization;
using StarterKit.Shared.Authorization;
using StarterKit.Shared.Extensions;

namespace StarterKit.Transfers.Api.Controllers;

/// <summary>
/// Transfer costs are Inventory's moving-average costs, so they are gated by Inventory's own
/// <c>view_cost</c> permission (or full control, like <c>[MustHavePermission]</c>).
/// </summary>
internal static class TransferCostAccess
{
    public static bool CanViewCost(this ClaimsPrincipal user) =>
        user.HasPermission(InventoryPermissions.Stock.ViewCost) || user.IsFullControl();
}
