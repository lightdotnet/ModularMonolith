using StarterKit.Modules.Identity.Application.Common.Models;
using StarterKit.Modules.Identity.Web.Admin;
using StarterKit.Shared.Constants;
using Xunit;

namespace Identity.Tests.Web.Admin;

public class RolePermissionClaimsTests
{
    private static ClaimDto Permission(string value) => new()
    {
        Type = ClaimTypeConstants.Permission,
        Value = value,
    };

    [Fact]
    public void Merge_ShouldReplaceKnownPermissionsAndKeepEverythingElse()
    {
        // Arrange
        ClaimDto[] existing =
        [
            Permission("users.view"),
            Permission("users.delete"),
            Permission("orders.view"),
            new() { Type = "dept", Value = "eng" },
        ];
        string[] known = ["users.view", "users.delete", "roles.view"];
        string[] selected = ["users.view", "roles.view", "not.defined"];

        // Act
        var merged = RolePermissionClaims.Merge(existing, selected, known);

        // Assert
        Assert.Equal(
            ["dept:eng", "permission:orders.view", "permission:roles.view", "permission:users.view"],
            merged.Select(c => $"{c.Type}:{c.Value}").Order());
    }

    [Fact]
    public void Merge_WhenNothingSelected_ShouldRemoveOnlyKnownPermissions()
    {
        // Arrange
        ClaimDto[] existing = [Permission("users.view"), Permission("orders.view")];

        // Act
        var merged = RolePermissionClaims.Merge(existing, [], ["users.view"]);

        // Assert
        var claim = Assert.Single(merged);
        Assert.Equal("orders.view", claim.Value);
    }
}
