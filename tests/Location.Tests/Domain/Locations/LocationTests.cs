using Light.Exceptions;
using StarterKit.Locations.Api.Domain.LocationTypes;
using StarterKit.Locations.Api.Domain.Locations;
using StarterKit.Locations.Contracts.Common;
using Xunit;

namespace Location.Tests.Domain.Locations;

/// <summary>
/// Covers <see cref="StarterKit.Locations.Api.Domain.Locations.Location"/> and
/// <see cref="LocationType"/> together — <c>Location</c>'s only remaining domain rule
/// (<c>ValidateParent</c>) is entirely driven by data on the parent's resolved
/// <see cref="LocationType"/> (<see cref="LocationType.AllowedParentTypeId"/> /
/// <see cref="LocationType.CanHaveChildren"/>), not a hardcoded switch.
/// </summary>
public class LocationTests
{
    private static LocationType RootType(string id = "STORE", bool canHaveChildren = true) =>
        LocationType.Create(id, id, null, canHaveChildren);

    private static LocationType ChildType(
        string id,
        string allowedParentTypeId,
        bool canHaveChildren = true) =>
        LocationType.Create(id, id, allowedParentTypeId, canHaveChildren);

    [Fact]
    public void Create_ShouldSucceed_WhenRootTypeHasNoParent()
    {
        // Arrange
        var type = RootType();

        // Act
        var location = StarterKit.Locations.Api.Domain.Locations.Location.Create(
            "Main Store", "STR-001", type, null, null);

        // Assert
        Assert.Equal("Main Store", location.Name);
        Assert.Equal("STR-001", location.Code);
        Assert.Equal(type.Id, location.LocationTypeId);
        Assert.Same(type, location.Type);
        Assert.Null(location.ParentLocationId);
        Assert.Equal(LocationStatus.Active, location.Status);
    }

    [Fact]
    public void Create_ShouldThrowValidationException_WhenRootTypeGivenParent()
    {
        // Arrange
        var rootType = RootType();
        var parentType = RootType("WAREHOUSE");
        var parent = StarterKit.Locations.Api.Domain.Locations.Location.Create(
            "Central Warehouse", "WH-001", parentType, null, null);

        // Act & Assert
        Assert.Throws<ValidationException>(
            () => StarterKit.Locations.Api.Domain.Locations.Location.Create(
                "Main Store", "STR-001", rootType, parent.Id, parent));
    }

    [Fact]
    public void Create_ShouldSucceed_WhenParentTypeMatchesAllowedParentType()
    {
        // Arrange
        var parentType = RootType("STORE");
        var parent = StarterKit.Locations.Api.Domain.Locations.Location.Create(
            "Main Store", "STR-001", parentType, null, null);
        var childType = ChildType("BIN", allowedParentTypeId: "STORE");

        // Act
        var child = StarterKit.Locations.Api.Domain.Locations.Location.Create(
            "Bin A", "BIN-A", childType, parent.Id, parent);

        // Assert
        Assert.Equal(parent.Id, child.ParentLocationId);
    }

    [Fact]
    public void Create_ShouldThrowValidationException_WhenParentTypeDoesNotMatchAllowedParentType()
    {
        // Arrange
        var wrongParentType = RootType("WAREHOUSE");
        var parent = StarterKit.Locations.Api.Domain.Locations.Location.Create(
            "Central Warehouse", "WH-001", wrongParentType, null, null);
        var childType = ChildType("BIN", allowedParentTypeId: "STORE");

        // Act & Assert
        Assert.Throws<ValidationException>(
            () => StarterKit.Locations.Api.Domain.Locations.Location.Create(
                "Bin A", "BIN-A", childType, parent.Id, parent));
    }

    [Fact]
    public void Create_ShouldThrowValidationException_WhenParentRequiredButNotGiven()
    {
        // Arrange
        var childType = ChildType("BIN", allowedParentTypeId: "STORE");

        // Act & Assert
        Assert.Throws<ValidationException>(
            () => StarterKit.Locations.Api.Domain.Locations.Location.Create(
                "Bin A", "BIN-A", childType, null, null));
    }

    [Fact]
    public void Create_ShouldThrowValidationException_WhenParentTypeCannotHaveChildren()
    {
        // Arrange
        var parentType = RootType("BIN", canHaveChildren: false);
        var parent = StarterKit.Locations.Api.Domain.Locations.Location.Create(
            "Bin A", "BIN-A", parentType, null, null);
        var childType = ChildType("SUBBIN", allowedParentTypeId: "BIN");

        // Act & Assert
        Assert.Throws<ValidationException>(
            () => StarterKit.Locations.Api.Domain.Locations.Location.Create(
                "Sub Bin", "SUBBIN-A", childType, parent.Id, parent));
    }

    [Fact]
    public void Move_ShouldSucceed_WhenNewParentTypeMatchesAllowedParentType()
    {
        // Arrange
        var storeType = RootType("STORE");
        var oldParent = StarterKit.Locations.Api.Domain.Locations.Location.Create(
            "Store A", "STR-A", storeType, null, null);
        var newParent = StarterKit.Locations.Api.Domain.Locations.Location.Create(
            "Store B", "STR-B", storeType, null, null);
        var childType = ChildType("BIN", allowedParentTypeId: "STORE");
        var child = StarterKit.Locations.Api.Domain.Locations.Location.Create(
            "Bin A", "BIN-A", childType, oldParent.Id, oldParent);

        // Act
        child.Move(childType, newParent.Id, newParent);

        // Assert
        Assert.Equal(newParent.Id, child.ParentLocationId);
    }

    [Fact]
    public void Move_ShouldThrowValidationException_WhenNewParentTypeDoesNotMatchAllowedParentType()
    {
        // Arrange
        var storeType = RootType("STORE");
        var oldParent = StarterKit.Locations.Api.Domain.Locations.Location.Create(
            "Store A", "STR-A", storeType, null, null);
        var warehouseType = RootType("WAREHOUSE");
        var wrongNewParent = StarterKit.Locations.Api.Domain.Locations.Location.Create(
            "Warehouse A", "WH-A", warehouseType, null, null);
        var childType = ChildType("BIN", allowedParentTypeId: "STORE");
        var child = StarterKit.Locations.Api.Domain.Locations.Location.Create(
            "Bin A", "BIN-A", childType, oldParent.Id, oldParent);

        // Act & Assert
        Assert.Throws<ValidationException>(
            () => child.Move(childType, wrongNewParent.Id, wrongNewParent));
    }

    [Fact]
    public void Update_ShouldSetNameCodeAndStatus()
    {
        // Arrange
        var type = RootType();
        var location = StarterKit.Locations.Api.Domain.Locations.Location.Create(
            "Main Store", "STR-001", type, null, null);

        // Act
        location.Update("Renamed Store", "STR-002", LocationStatus.Inactive);

        // Assert
        Assert.Equal("Renamed Store", location.Name);
        Assert.Equal("STR-002", location.Code);
        Assert.Equal(LocationStatus.Inactive, location.Status);
    }

    [Fact]
    public void Update_ShouldLeaveTypeAndParentUnchanged()
    {
        // Arrange
        var type = RootType();
        var location = StarterKit.Locations.Api.Domain.Locations.Location.Create(
            "Main Store", "STR-001", type, null, null);

        // Act
        location.Update("Renamed Store", "STR-002", LocationStatus.Active);

        // Assert
        Assert.Same(type, location.Type);
        Assert.Null(location.ParentLocationId);
    }

    [Fact]
    public void LocationTypeCreate_ShouldSetAllFields()
    {
        // Act
        var type = LocationType.Create("STORE", "Store", null, canHaveChildren: true);

        // Assert
        Assert.Equal("STORE", type.Id);
        Assert.Equal("Store", type.Name);
        Assert.Null(type.AllowedParentTypeId);
        Assert.True(type.CanHaveChildren);
        Assert.Equal(LocationTypeStatus.Active, type.Status);
    }

    [Fact]
    public void LocationTypeUpdate_ShouldSetAllFields()
    {
        // Arrange
        var type = LocationType.Create("BIN", "Bin", "STORE", canHaveChildren: false);

        // Act
        type.Update("Bin (renamed)", "WAREHOUSE", canHaveChildren: true, LocationTypeStatus.Inactive);

        // Assert
        Assert.Equal("Bin (renamed)", type.Name);
        Assert.Equal("WAREHOUSE", type.AllowedParentTypeId);
        Assert.True(type.CanHaveChildren);
        Assert.Equal(LocationTypeStatus.Inactive, type.Status);
    }
}
