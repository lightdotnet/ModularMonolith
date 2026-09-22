using Location.Tests.TestSupport;
using StarterKit.Locations.Api.Application.LocationTypes.Queries;
using StarterKit.Locations.Api.Domain.LocationTypes;
using Xunit;

namespace Location.Tests.Application.LocationTypes.Queries;

public class GetLocationTypeByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnType_WhenFound()
    {
        // Arrange
        using var host = new LocationTestHost();
        var storeType = LocationType.Create("STORE", "Store", null, canHaveChildren: true);
        await host.Context.LocationTypes.AddAsync(storeType, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetLocationTypeByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetLocationTypeByIdQuery("STORE"), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Store", result.Data.Name);
        Assert.True(result.Data.CanHaveChildren);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenTypeDoesNotExist()
    {
        // Arrange
        using var host = new LocationTestHost();
        var handler = new GetLocationTypeByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetLocationTypeByIdQuery("missing"), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }
}
