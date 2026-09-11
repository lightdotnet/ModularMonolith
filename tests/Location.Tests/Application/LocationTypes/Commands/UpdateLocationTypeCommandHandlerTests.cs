using Location.Tests.TestSupport;
using Moq;
using StarterKit.Locations.Api.Application.LocationTypes.Commands;
using StarterKit.Locations.Api.Domain.LocationTypes;
using StarterKit.Locations.Api.Services;
using StarterKit.Locations.Contracts.Common;
using StarterKit.Locations.Contracts.LocationTypes;
using Xunit;

namespace Location.Tests.Application.LocationTypes.Commands;

public class UpdateLocationTypeCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenTypeDoesNotExist()
    {
        // Arrange
        using var host = new LocationTestHost();
        var cacheMock = new Mock<ILocationTypeCache>();
        var handler = new UpdateLocationTypeCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new UpdateLocationTypeCommand(
                "missing",
                new UpdateLocationTypeRequest { Name = "X", CanHaveChildren = true, Status = LocationTypeStatus.Active }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        cacheMock.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenAllowedParentTypeIsSelf()
    {
        // Arrange
        using var host = new LocationTestHost();
        await SeedTypeAsync(host, "BIN");
        var cacheMock = new Mock<ILocationTypeCache>();
        var handler = new UpdateLocationTypeCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new UpdateLocationTypeCommand(
                "BIN",
                new UpdateLocationTypeRequest
                {
                    Name = "Bin", AllowedParentTypeId = "BIN", CanHaveChildren = false, Status = LocationTypeStatus.Active,
                }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        cacheMock.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenAllowedParentTypeIdDoesNotExist()
    {
        // Arrange
        using var host = new LocationTestHost();
        await SeedTypeAsync(host, "BIN");
        var cacheMock = new Mock<ILocationTypeCache>();
        var handler = new UpdateLocationTypeCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new UpdateLocationTypeCommand(
                "BIN",
                new UpdateLocationTypeRequest
                {
                    Name = "Bin", AllowedParentTypeId = "MISSING", CanHaveChildren = false, Status = LocationTypeStatus.Active,
                }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        cacheMock.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldUpdate_AndReloadCache_WhenValid()
    {
        // Arrange
        using var host = new LocationTestHost();
        await SeedTypeAsync(host, "STORE");
        await SeedTypeAsync(host, "BIN");
        var cacheMock = new Mock<ILocationTypeCache>();
        var handler = new UpdateLocationTypeCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new UpdateLocationTypeCommand(
                "BIN",
                new UpdateLocationTypeRequest
                {
                    Name = "Bin (renamed)",
                    AllowedParentTypeId = "STORE",
                    CanHaveChildren = true,
                    Status = LocationTypeStatus.Inactive,
                }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var updated = await host.Context.LocationTypes.FindAsync(["BIN"], TestContext.Current.CancellationToken);
        Assert.Equal("Bin (renamed)", updated!.Name);
        Assert.Equal("STORE", updated.AllowedParentTypeId);
        Assert.True(updated.CanHaveChildren);
        Assert.Equal(LocationTypeStatus.Inactive, updated.Status);
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
}
