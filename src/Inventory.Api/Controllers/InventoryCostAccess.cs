using System.Security.Claims;
using StarterKit.Inventory.Contracts.Authorization;
using StarterKit.Shared.Authorization;
using StarterKit.Shared.Extensions;

namespace StarterKit.Inventory.Api.Controllers;

/// <summary>The single rule deciding whether a caller may see cost fields.</summary>
internal static class InventoryCostAccess
{
    // Same semantics as [MustHavePermission]: the permission itself, or full control.
    public static bool CanViewCost(this ClaimsPrincipal user) =>
        user.HasPermission(InventoryPermissions.Stock.ViewCost) || user.IsFullControl();
}
