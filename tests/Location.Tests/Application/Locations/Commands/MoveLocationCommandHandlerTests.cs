using Location.Tests.TestSupport;
using Moq;
using StarterKit.Locations.Api.Application.Locations.Commands;
using StarterKit.Locations.Api.Domain.LocationTypes;
using StarterKit.Locations.Api.Services;
using StarterKit.Locations.Contracts.Locations;
using Xunit;
using LocationEntity = StarterKit.Locations.Api.Domain.Locations.Location;
using ValidationException = Light.Exceptions.ValidationException;

namespace Location.Tests.Application.Locations.Commands;

public class MoveLocationCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenLocationDoesNotExist()
    {
        // Arrange
        using var host = new LocationTestHost();
        var cacheMock = new Mock<ILocationTypeCache>();
        var handler = new MoveLocationCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new MoveLocationCommand("missing", new MoveLocationRequest { NewParentLocationId = null }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenLocationTypeNotFoundInCache()
    {
        // Arrange
        using var host = new LocationTestHost();
        var storeType = await SeedTypeAsync(host, "STORE");
        var store = await SeedLocationAsync(host, storeType, "Store 1", "S1");
        var cacheMock = new Mock<ILocationTypeCache>();
        cacheMock
            .Setup(x => x.GetAsync("STORE", It.IsAny<CancellationToken>()))
            .ReturnsAsync((LocationType?)null);
        var handler = new MoveLocationCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new MoveLocationCommand(store.Id, new MoveLocationRequest { NewParentLocationId = null }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenNewParentIsSelf()
    {
        // Arrange
        using var host = new LocationTestHost();
        var storeType = await SeedTypeAsync(host, "STORE");
        var store = await SeedLocationAsync(host, storeType, "Store 1", "S1");
        var cacheMock = CacheReturning(storeType);
        var handler = new MoveLocationCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new MoveLocationCommand(store.Id, new MoveLocationRequest { NewParentLocationId = store.Id }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenNewParentDoesNotExist()
    {
        // Arrange
        using var host = new LocationTestHost();
        var storeType = await SeedTypeAsync(host, "STORE");
        var store = await SeedLocationAsync(host, storeType, "Store 1", "S1");
        var cacheMock = CacheReturning(storeType);
        var handler = new MoveLocationCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new MoveLocationCommand(store.Id, new MoveLocationRequest { NewParentLocationId = "missing" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenNewParentIsOwnDescendant()
    {
        // Arrange: root -> child -> grandchild (three distinct types satisfying the allowed-parent
        // chain), moving root under grandchild must be rejected. The cycle guard short-circuits
        // before Move's own type-hierarchy re-validation would even run, so the type mismatch that
        // moving root under grandchild would otherwise imply never gets evaluated.
        using var host = new LocationTestHost();
        var rootType = await SeedTypeAsync(host, "ROOT");
        var midType = await SeedTypeAsync(host, "MID", allowedParentTypeId: "ROOT");
        var leafType = await SeedTypeAsync(host, "LEAF", allowedParentTypeId: "MID");
        var root = await SeedLocationAsync(host, rootType, "Root", "ROOT-1");
        var child = await SeedLocationAsync(host, midType, "Child", "MID-1", root);
        var grandchild = await SeedLocationAsync(host, leafType, "Grandchild", "LEAF-1", child);
        var cacheMock = CacheReturning(rootType);
        var handler = new MoveLocationCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new MoveLocationCommand(root.Id, new MoveLocationRequest { NewParentLocationId = grandchild.Id }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldClearParent_WhenNewParentIdIsEmpty()
    {
        // Arrange
        using var host = new LocationTestHost();
        var storeType = await SeedTypeAsync(host, "STORE");
        var store = await SeedLocationAsync(host, storeType, "Store 1", "S1");
        var cacheMock = CacheReturning(storeType);
        var handler = new MoveLocationCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new MoveLocationCommand(store.Id, new MoveLocationRequest { NewParentLocationId = null }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var updated = await host.Context.Locations.FindAsync([store.Id], TestContext.Current.CancellationToken);
        Assert.Null(updated!.ParentLocationId);
    }

    [Fact]
    public async Task Handle_ShouldMove_WhenTargetIsValidSiblingSubtree()
    {
        // Arrange: two independent STORE roots; move a BIN from storeA to storeB.
        using var host = new LocationTestHost();
        var storeType = await SeedTypeAsync(host, "STORE");
        var binType = await SeedTypeAsync(host, "BIN", allowedParentTypeId: "STORE");
        var storeA = await SeedLocationAsync(host, storeType, "Store A", "S-A");
        var storeB = await SeedLocationAsync(host, storeType, "Store B", "S-B");
        var bin = await SeedLocationAsync(host, binType, "Bin A", "BIN-A", storeA);
        var cacheMock = CacheReturning(binType);
        var handler = new MoveLocationCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new MoveLocationCommand(bin.Id, new MoveLocationRequest { NewParentLocationId = storeB.Id }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var updated = await host.Context.Locations.FindAsync([bin.Id], TestContext.Current.CancellationToken);
        Assert.Equal(storeB.Id, updated!.ParentLocationId);
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenNewParentTypeHierarchyViolated()
    {
        // Arrange: BIN only allows a STORE parent; moving it under a WAREHOUSE is a domain violation.
        using var host = new LocationTestHost();
        var storeType = await SeedTypeAsync(host, "STORE");
        var warehouseType = await SeedTypeAsync(host, "WAREHOUSE");
        var binType = await SeedTypeAsync(host, "BIN", allowedParentTypeId: "STORE");
        var store = await SeedLocationAsync(host, storeType, "Store A", "S-A");
        var warehouse = await SeedLocationAsync(host, warehouseType, "Warehouse A", "WH-A");
        var bin = await SeedLocationAsync(host, binType, "Bin A", "BIN-A", store);
        var cacheMock = CacheReturning(binType);
        var handler = new MoveLocationCommandHandler(host.Context, cacheMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(
                new MoveLocationCommand(bin.Id, new MoveLocationRequest { NewParentLocationId = warehouse.Id }),
                TestContext.Current.CancellationToken));
    }

    private static Mock<ILocationTypeCache> CacheReturning(LocationType type)
    {
        var mock = new Mock<ILocationTypeCache>();
        mock.Setup(x => x.GetAsync(type.Id, It.IsAny<CancellationToken>())).ReturnsAsync(type);
        return mock;
    }

    private static async Task<LocationType> SeedTypeAsync(
        LocationTestHost host,
        string id,
        string? allowedParentTypeId = null,
        bool canHaveChildren = true)
    {
        var type = LocationType.Create(id, id, allowedParentTypeId, canHaveChildren);
        await host.Context.LocationTypes.AddAsync(type, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return type;
    }

    private static async Task<LocationEntity> SeedLocationAsync(
        LocationTestHost host,
        LocationType type,
        string name,
        string code,
        LocationEntity? parent = null)
    {
        var location = LocationEntity.Create(name, code, type, parent?.Id, parent);
        await host.Context.Locations.AddAsync(location, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return location;
    }
}
