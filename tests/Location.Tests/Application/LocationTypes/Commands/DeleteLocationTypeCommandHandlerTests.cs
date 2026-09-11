using Location.Tests.TestSupport;
using Moq;
using StarterKit.Locations.Api.Application.LocationTypes.Commands;
using StarterKit.Locations.Api.Domain.LocationTypes;
using StarterKit.Locations.Api.Services;
using Xunit;
using LocationEntity = StarterKit.Locations.Api.Domain.Locations.Location;

namespace Location.Tests.Application.LocationTypes.Commands;

public class DeleteLocationTypeCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenTypeDoesNotExist()
    {
        // Arrange
        using var host = new LocationTestHost();
        var cacheMock = new Mock<ILocationTypeCache>();
        var handler = new DeleteLocationTypeCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(new DeleteLocationTypeCommand("missing"), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        cacheMock.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenInUseByLocation()
    {
        // Arrange
        using var host = new LocationTestHost();
        var storeType = await SeedTypeAsync(host, "STORE");
        await SeedLocationAsync(host, storeType, "Store 1", "S1");
        var cacheMock = new Mock<ILocationTypeCache>();
        var handler = new DeleteLocationTypeCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(new DeleteLocationTypeCommand("STORE"), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        cacheMock.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenReferencedByOtherTypeAsAllowedParent()
    {
        // Arrange
        using var host = new LocationTestHost();
        await SeedTypeAsync(host, "STORE");
        await SeedTypeAsync(host, "BIN", allowedParentTypeId: "STORE");
        var cacheMock = new Mock<ILocationTypeCache>();
        var handler = new DeleteLocationTypeCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(new DeleteLocationTypeCommand("STORE"), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        cacheMock.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldDelete_AndReloadCache_WhenNotInUse()
    {
        // Arrange
        using var host = new LocationTestHost();
        await SeedTypeAsync(host, "STORE");
        var cacheMock = new Mock<ILocationTypeCache>();
        var handler = new DeleteLocationTypeCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(new DeleteLocationTypeCommand("STORE"), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(await host.Context.LocationTypes.FindAsync(["STORE"], TestContext.Current.CancellationToken));
        cacheMock.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Once);
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
