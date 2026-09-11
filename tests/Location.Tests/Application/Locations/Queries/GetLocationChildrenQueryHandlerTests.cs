using Location.Tests.TestSupport;
using StarterKit.Locations.Api.Application.Locations.Queries;
using StarterKit.Locations.Api.Domain.LocationTypes;
using Xunit;
using LocationEntity = StarterKit.Locations.Api.Domain.Locations.Location;

namespace Location.Tests.Application.Locations.Queries;

public class GetLocationChildrenQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnChildren_WhenParentHasChildren()
    {
        // Arrange
        using var host = new LocationTestHost();
        var storeType = LocationType.Create("STORE", "Store", null, canHaveChildren: true);
        var binType = LocationType.Create("BIN", "Bin", "STORE", canHaveChildren: false);
        await host.Context.LocationTypes.AddRangeAsync(storeType, binType);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var store = LocationEntity.Create("Store 1", "S1", storeType, null, null);
        await host.Context.Locations.AddAsync(store, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var bin = LocationEntity.Create("Bin A", "BIN-A", binType, store.Id, store);
        await host.Context.Locations.AddAsync(bin, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetLocationChildrenQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetLocationChildrenQuery(store.Id), TestContext.Current.CancellationToken);

        // Assert
        var child = Assert.Single(result);
        Assert.Equal(bin.Id, child.Id);
        Assert.Equal("BIN-A", child.Code);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenNoChildren()
    {
        // Arrange
        using var host = new LocationTestHost();
        var storeType = LocationType.Create("STORE", "Store", null, canHaveChildren: true);
        await host.Context.LocationTypes.AddAsync(storeType, TestContext.Current.CancellationToken);
        var store = LocationEntity.Create("Store 1", "S1", storeType, null, null);
        await host.Context.Locations.AddAsync(store, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetLocationChildrenQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetLocationChildrenQuery(store.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
    }
}
