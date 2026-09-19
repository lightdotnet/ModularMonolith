using Moq;
using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Application.OrderTypes.Commands;
using StarterKit.Orders.Api.Domain.OrderTypes;
using StarterKit.Orders.Api.Services;
using StarterKit.Orders.Contracts.Common;
using StarterKit.Orders.Contracts.OrderTypes;
using Xunit;

namespace Orders.Tests.Application.OrderTypes.Commands;

public class UpdateOrderTypeCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenTypeDoesNotExist()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var cacheMock = new Mock<IOrderTypeCache>();
        var handler = new UpdateOrderTypeCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new UpdateOrderTypeCommand(
                "missing",
                OrderTypeCategory.Fee,
                new UpdateOrderTypeRequest { Name = "X", Status = OrderTypeStatus.Active }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        cacheMock.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Proves the lookup is keyed by the full composite <c>(Id, Category)</c> pair — a matching Id
    /// under a different category must not be found.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenIdExistsOnlyUnderADifferentCategory()
    {
        // Arrange
        using var host = new OrdersTestHost();
        await SeedTypeAsync(host, "OTHER", OrderTypeCategory.Fee);
        var cacheMock = new Mock<IOrderTypeCache>();
        var handler = new UpdateOrderTypeCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new UpdateOrderTypeCommand(
                "OTHER",
                OrderTypeCategory.Payment,
                new UpdateOrderTypeRequest { Name = "Other", Status = OrderTypeStatus.Active }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        cacheMock.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldUpdate_AndReloadCache_WhenValid()
    {
        // Arrange
        using var host = new OrdersTestHost();
        await SeedTypeAsync(host, "SHIPPING", OrderTypeCategory.Fee);
        var cacheMock = new Mock<IOrderTypeCache>();
        var handler = new UpdateOrderTypeCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new UpdateOrderTypeCommand(
                "SHIPPING",
                OrderTypeCategory.Fee,
                new UpdateOrderTypeRequest { Name = "Shipping (renamed)", Status = OrderTypeStatus.Inactive }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var updated = await host.Context.OrderTypes.FindAsync(["SHIPPING", OrderTypeCategory.Fee], TestContext.Current.CancellationToken);
        Assert.Equal("Shipping (renamed)", updated!.Name);
        Assert.Equal(OrderTypeStatus.Inactive, updated.Status);
        cacheMock.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static async Task<OrderType> SeedTypeAsync(OrdersTestHost host, string id, OrderTypeCategory category)
    {
        var type = OrderType.Create(id, category, id);
        await host.Context.OrderTypes.AddAsync(type, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return type;
    }
}
