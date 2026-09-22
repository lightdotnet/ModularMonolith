using System.Security.Claims;
using StarterKit.Inventory.Api.Controllers;
using StarterKit.Inventory.Contracts.Authorization;
using StarterKit.Shared.Authorization;
using StarterKit.Shared.Constants;
using Xunit;

namespace Inventory.Tests.Controllers;

public class InventoryCostAccessTests
{
    public static ClaimsPrincipal PrincipalWith(
        string? userName = null,
        params string[] permissions)
    {
        var claims = permissions
            .Select(x => new Claim(ClaimTypeConstants.Permission, x))
            .ToList();

        if (userName is not null)
            claims.Add(new Claim(ClaimTypeConstants.UserName, userName));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    [Fact]
    public void CanViewCost_ShouldBeFalse_WhenTheCallerHasNoPermission()
    {
        Assert.False(PrincipalWith().CanViewCost());
    }

    [Fact]
    public void CanViewCost_ShouldBeFalse_ForOtherInventoryPermissions()
    {
        var user = PrincipalWith(null, InventoryPermissions.Stock.View, InventoryPermissions.Stock.Manage, InventoryPermissions.Stock.Revalue);

        Assert.False(user.CanViewCost());
    }

    [Fact]
    public void CanViewCost_ShouldBeTrue_WithTheViewCostPermission()
    {
        Assert.True(PrincipalWith(null, InventoryPermissions.Stock.ViewCost).CanViewCost());
    }

    [Fact]
    public void CanViewCost_ShouldBeTrue_ForFullControl()
    {
        Assert.True(PrincipalWith(SuperUserPolicy.SuperUserName).CanViewCost());
    }

    [Fact]
    public void CanViewCost_ShouldBeFalse_ForAnOrdinaryUserNameWithoutPermission()
    {
        Assert.False(PrincipalWith("someone").CanViewCost());
    }
}
