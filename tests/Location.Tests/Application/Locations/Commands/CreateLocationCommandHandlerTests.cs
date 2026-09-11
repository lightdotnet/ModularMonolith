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

public class CreateLocationCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenLocationTypeNotFound()
    {
        // Arrange
        using var host = new LocationTestHost();
        var cacheMock = new Mock<ILocationTypeCache>();
        cacheMock
            .Setup(x => x.GetAsync("STORE", It.IsAny<CancellationToken>()))
            .ReturnsAsync((LocationType?)null);
        var handler = new CreateLocationCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new CreateLocationCommand(new CreateLocationRequest { LocationTypeId = "STORE", Name = "Store 1", Code = "S1" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenParentLocationDoesNotExist()
    {
        // Arrange
        using var host = new LocationTestHost();
        var storeType = await SeedTypeAsync(host, "STORE");
        var cacheMock = CacheReturning(storeType);
        var handler = new CreateLocationCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new CreateLocationCommand(new CreateLocationRequest
            {
                LocationTypeId = "STORE", Name = "Store 1", Code = "S1", ParentLocationId = "missing",
            }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenCodeAlreadyExists()
    {
        // Arrange
        using var host = new LocationTestHost();
        var storeType = await SeedTypeAsync(host, "STORE");
        await SeedLocationAsync(host, storeType, "Store 1", "S1");
        var cacheMock = CacheReturning(storeType);
        var handler = new CreateLocationCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new CreateLocationCommand(new CreateLocationRequest { LocationTypeId = "STORE", Name = "Store 2", Code = "S1" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldCreate_WhenRootTypeWithNoParent()
    {
        // Arrange
        using var host = new LocationTestHost();
        var storeType = await SeedTypeAsync(host, "STORE");
        var cacheMock = CacheReturning(storeType);
        var handler = new CreateLocationCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new CreateLocationCommand(new CreateLocationRequest { LocationTypeId = "STORE", Name = "Store 1", Code = "S1" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var saved = await host.Context.Locations.FindAsync([result.Data], TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal("S1", saved!.Code);
        Assert.Null(saved.ParentLocationId);
    }

    [Fact]
    public async Task Handle_ShouldCreate_WhenParentTypeMatchesAllowedParentType()
    {
        // Arrange
        using var host = new LocationTestHost();
        var storeType = await SeedTypeAsync(host, "STORE");
        var binType = await SeedTypeAsync(host, "BIN", allowedParentTypeId: "STORE");
        var store = await SeedLocationAsync(host, storeType, "Store 1", "S1");
        var cacheMock = CacheReturning(binType);
        var handler = new CreateLocationCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new CreateLocationCommand(new CreateLocationRequest
            {
                LocationTypeId = "BIN", Name = "Bin A", Code = "BIN-A", ParentLocationId = store.Id,
            }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var saved = await host.Context.Locations.FindAsync([result.Data], TestContext.Current.CancellationToken);
        Assert.Equal(store.Id, saved!.ParentLocationId);
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenParentTypeHierarchyViolated()
    {
        // Arrange: STORE is a root type (no allowed parent) but is given a parent — invalid.
        using var host = new LocationTestHost();
        var storeType = await SeedTypeAsync(host, "STORE");
        var warehouseType = await SeedTypeAsync(host, "WAREHOUSE");
        var warehouse = await SeedLocationAsync(host, warehouseType, "Warehouse 1", "WH-1");
        var cacheMock = CacheReturning(storeType);
        var handler = new CreateLocationCommandHandler(host.Context, cacheMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(
                new CreateLocationCommand(new CreateLocationRequest
                {
                    LocationTypeId = "STORE", Name = "Store 1", Code = "S1", ParentLocationId = warehouse.Id,
                }),
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
