using Location.Tests.TestSupport;
using StarterKit.Locations.Api.Application.LocationTypes.Queries;
using StarterKit.Locations.Api.Domain.LocationTypes;
using Xunit;

namespace Location.Tests.Application.LocationTypes.Queries;

public class GetLocationTypesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenNoneExist()
    {
        // Arrange
        using var host = new LocationTestHost();
        var handler = new GetLocationTypesQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetLocationTypesQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_ShouldReturnAllTypes_OrderedByName()
    {
        // Arrange
        using var host = new LocationTestHost();
        var warehouseType = LocationType.Create("WAREHOUSE", "Warehouse", null, canHaveChildren: true);
        var binType = LocationType.Create("BIN", "Bin", null, canHaveChildren: false);
        await host.Context.LocationTypes.AddRangeAsync(warehouseType, binType);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetLocationTypesQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetLocationTypesQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(["Bin", "Warehouse"], result.Select(x => x.Name));
    }
}
