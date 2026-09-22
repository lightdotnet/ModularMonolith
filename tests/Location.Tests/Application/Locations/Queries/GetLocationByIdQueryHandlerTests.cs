using Location.Tests.TestSupport;
using StarterKit.Locations.Api.Application.Locations.Queries;
using StarterKit.Locations.Api.Domain.LocationTypes;
using Xunit;
using LocationEntity = StarterKit.Locations.Api.Domain.Locations.Location;

namespace Location.Tests.Application.Locations.Queries;

public class GetLocationByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnLocation_WhenFound()
    {
        // Arrange
        using var host = new LocationTestHost();
        var storeType = LocationType.Create("STORE", "Store", null, canHaveChildren: true);
        await host.Context.LocationTypes.AddAsync(storeType, TestContext.Current.CancellationToken);
        var store = LocationEntity.Create("Store 1", "S1", storeType, null, null);
        await host.Context.Locations.AddAsync(store, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetLocationByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetLocationByIdQuery(store.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("S1", result.Data.Code);
        Assert.Equal("STORE", result.Data.LocationTypeId);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenLocationDoesNotExist()
    {
        // Arrange
        using var host = new LocationTestHost();
        var handler = new GetLocationByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetLocationByIdQuery("missing"), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }
}
