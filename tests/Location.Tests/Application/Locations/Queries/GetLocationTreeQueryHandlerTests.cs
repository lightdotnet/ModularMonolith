using Location.Tests.TestSupport;
using StarterKit.Locations.Api.Application.Locations.Queries;
using StarterKit.Locations.Api.Domain.LocationTypes;
using Xunit;
using LocationEntity = StarterKit.Locations.Api.Domain.Locations.Location;

namespace Location.Tests.Application.Locations.Queries;

public class GetLocationTreeQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenNoLocations()
    {
        // Arrange
        using var host = new LocationTestHost();
        var handler = new GetLocationTreeQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetLocationTreeQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_ShouldBuildMultiLevelTree()
    {
        // Arrange: store -> bin, plus a second independent root store.
        using var host = new LocationTestHost();
        var storeType = LocationType.Create("STORE", "Store", null, canHaveChildren: true);
        var binType = LocationType.Create("BIN", "Bin", "STORE", canHaveChildren: false);
        await host.Context.LocationTypes.AddRangeAsync(storeType, binType);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var storeA = LocationEntity.Create("Store A", "S-A", storeType, null, null);
        var storeB = LocationEntity.Create("Store B", "S-B", storeType, null, null);
        await host.Context.Locations.AddRangeAsync(storeA, storeB);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var bin = LocationEntity.Create("Bin A", "BIN-A", binType, storeA.Id, storeA);
        await host.Context.Locations.AddAsync(bin, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetLocationTreeQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetLocationTreeQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        var storeANode = Assert.Single(result, x => x.Id == storeA.Id);
        var binNode = Assert.Single(storeANode.Children);
        Assert.Equal(bin.Id, binNode.Id);
        var storeBNode = Assert.Single(result, x => x.Id == storeB.Id);
        Assert.Empty(storeBNode.Children);
    }
}
