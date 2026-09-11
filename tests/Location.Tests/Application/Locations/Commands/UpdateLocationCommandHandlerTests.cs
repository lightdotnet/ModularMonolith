using Location.Tests.TestSupport;
using StarterKit.Locations.Api.Application.Locations.Commands;
using StarterKit.Locations.Api.Domain.LocationTypes;
using StarterKit.Locations.Contracts.Common;
using StarterKit.Locations.Contracts.Locations;
using Xunit;
using LocationEntity = StarterKit.Locations.Api.Domain.Locations.Location;

namespace Location.Tests.Application.Locations.Commands;

public class UpdateLocationCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenLocationDoesNotExist()
    {
        // Arrange
        using var host = new LocationTestHost();
        var handler = new UpdateLocationCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new UpdateLocationCommand(
                "missing",
                new UpdateLocationRequest { Name = "New Name", Code = "NEW", Status = LocationStatus.Active }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenCodeAlreadyTakenByAnotherLocation()
    {
        // Arrange
        using var host = new LocationTestHost();
        var storeType = await SeedTypeAsync(host, "STORE");
        await SeedLocationAsync(host, storeType, "Store 1", "S1");
        var other = await SeedLocationAsync(host, storeType, "Store 2", "S2");
        var handler = new UpdateLocationCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new UpdateLocationCommand(
                other.Id,
                new UpdateLocationRequest { Name = "Store 2", Code = "S1", Status = LocationStatus.Active }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldUpdate_WhenCodeUnchanged()
    {
        // Arrange
        using var host = new LocationTestHost();
        var storeType = await SeedTypeAsync(host, "STORE");
        var store = await SeedLocationAsync(host, storeType, "Store 1", "S1");
        var handler = new UpdateLocationCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new UpdateLocationCommand(
                store.Id,
                new UpdateLocationRequest { Name = "Store One", Code = "S1", Status = LocationStatus.Inactive }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var updated = await host.Context.Locations.FindAsync([store.Id], TestContext.Current.CancellationToken);
        Assert.Equal("Store One", updated!.Name);
        Assert.Equal("S1", updated.Code);
        Assert.Equal(LocationStatus.Inactive, updated.Status);
    }

    [Fact]
    public async Task Handle_ShouldUpdate_WhenCodeChangedToUniqueValue()
    {
        // Arrange
        using var host = new LocationTestHost();
        var storeType = await SeedTypeAsync(host, "STORE");
        var store = await SeedLocationAsync(host, storeType, "Store 1", "S1");
        var handler = new UpdateLocationCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new UpdateLocationCommand(
                store.Id,
                new UpdateLocationRequest { Name = "Store 1", Code = "S1-NEW", Status = LocationStatus.Active }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var updated = await host.Context.Locations.FindAsync([store.Id], TestContext.Current.CancellationToken);
        Assert.Equal("S1-NEW", updated!.Code);
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
