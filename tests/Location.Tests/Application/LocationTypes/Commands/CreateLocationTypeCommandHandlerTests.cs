using Location.Tests.TestSupport;
using Moq;
using StarterKit.Locations.Api.Application.LocationTypes.Commands;
using StarterKit.Locations.Api.Domain.LocationTypes;
using StarterKit.Locations.Api.Services;
using StarterKit.Locations.Contracts.LocationTypes;
using Xunit;

namespace Location.Tests.Application.LocationTypes.Commands;

public class CreateLocationTypeCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReject_WhenIdAlreadyExists()
    {
        // Arrange
        using var host = new LocationTestHost();
        await SeedTypeAsync(host, "STORE");
        var cacheMock = new Mock<ILocationTypeCache>();
        var handler = new CreateLocationTypeCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new CreateLocationTypeCommand(new CreateLocationTypeRequest { Id = "STORE", Name = "Store", CanHaveChildren = true }),
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
        var cacheMock = new Mock<ILocationTypeCache>();
        var handler = new CreateLocationTypeCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new CreateLocationTypeCommand(new CreateLocationTypeRequest
            {
                Id = "BIN", Name = "Bin", AllowedParentTypeId = "MISSING", CanHaveChildren = false,
            }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        cacheMock.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreate_AndReloadCache_WhenValid()
    {
        // Arrange
        using var host = new LocationTestHost();
        var cacheMock = new Mock<ILocationTypeCache>();
        var handler = new CreateLocationTypeCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new CreateLocationTypeCommand(new CreateLocationTypeRequest { Id = "STORE", Name = "Store", CanHaveChildren = true }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("STORE", result.Data);
        Assert.NotNull(await host.Context.LocationTypes.FindAsync(["STORE"], TestContext.Current.CancellationToken));
        cacheMock.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCreate_WhenAllowedParentTypeIdReferencesExistingType()
    {
        // Arrange
        using var host = new LocationTestHost();
        await SeedTypeAsync(host, "STORE");
        var cacheMock = new Mock<ILocationTypeCache>();
        var handler = new CreateLocationTypeCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new CreateLocationTypeCommand(new CreateLocationTypeRequest
            {
                Id = "BIN", Name = "Bin", AllowedParentTypeId = "STORE", CanHaveChildren = false,
            }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
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
