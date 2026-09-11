using Location.Tests.TestSupport;
using StarterKit.Locations.Api.Application.Locations.Commands;
using StarterKit.Locations.Api.Domain.LocationTypes;
using Xunit;
using LocationEntity = StarterKit.Locations.Api.Domain.Locations.Location;

namespace Location.Tests.Application.Locations.Commands;

public class DeleteLocationCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenLocationDoesNotExist()
    {
        // Arrange
        using var host = new LocationTestHost();
        var handler = new DeleteLocationCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(new DeleteLocationCommand("missing"), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenLocationHasChildren()
    {
        // Arrange
        using var host = new LocationTestHost();
        var storeType = await SeedTypeAsync(host, "STORE");
        var binType = await SeedTypeAsync(host, "BIN", allowedParentTypeId: "STORE");
        var store = await SeedLocationAsync(host, storeType, "Store 1", "S1");
        await SeedLocationAsync(host, binType, "Bin A", "BIN-A", store);
        var handler = new DeleteLocationCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(new DeleteLocationCommand(store.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(await host.Context.Locations.FindAsync([store.Id], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ShouldDelete_WhenLocationHasNoChildren()
    {
        // Arrange
        using var host = new LocationTestHost();
        var storeType = await SeedTypeAsync(host, "STORE");
        var store = await SeedLocationAsync(host, storeType, "Store 1", "S1");
        var handler = new DeleteLocationCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(new DeleteLocationCommand(store.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(await host.Context.Locations.FindAsync([store.Id], TestContext.Current.CancellationToken));
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
