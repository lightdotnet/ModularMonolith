using Moq;
using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Application.OrderTypes.Commands;
using StarterKit.Orders.Api.Domain.OrderTypes;
using StarterKit.Orders.Api.Services;
using StarterKit.Orders.Contracts.Common;
using StarterKit.Orders.Contracts.OrderTypes;
using Xunit;

namespace Orders.Tests.Application.OrderTypes.Commands;

public class CreateOrderTypeCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReject_WhenIdAlreadyExistsForSameCategory()
    {
        // Arrange
        using var host = new OrdersTestHost();
        await SeedTypeAsync(host, "SHIPPING", OrderTypeCategory.Fee);
        var cacheMock = new Mock<IOrderTypeCache>();
        var handler = new CreateOrderTypeCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new CreateOrderTypeCommand(
                new CreateOrderTypeRequest { Id = "SHIPPING", Category = OrderTypeCategory.Fee, Name = "Shipping" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        cacheMock.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// The composite <c>(Id, Category)</c> key means the same id is allowed to exist once per
    /// category — both catalogs legitimately seed the colliding code "OTHER".
    /// </summary>
    [Fact]
    public async Task Handle_ShouldCreate_WhenSameIdAlreadyExistsUnderADifferentCategory()
    {
        // Arrange
        using var host = new OrdersTestHost();
        await SeedTypeAsync(host, "OTHER", OrderTypeCategory.Fee);
        var cacheMock = new Mock<IOrderTypeCache>();
        var handler = new CreateOrderTypeCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new CreateOrderTypeCommand(
                new CreateOrderTypeRequest { Id = "OTHER", Category = OrderTypeCategory.Payment, Name = "Other" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        cacheMock.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCreate_AndReloadCache_WhenValid()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var cacheMock = new Mock<IOrderTypeCache>();
        var handler = new CreateOrderTypeCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new CreateOrderTypeCommand(
                new CreateOrderTypeRequest { Id = "SHIPPING", Category = OrderTypeCategory.Fee, Name = "Shipping" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("SHIPPING", result.Data);
        Assert.NotNull(await host.Context.OrderTypes.FindAsync(["SHIPPING", OrderTypeCategory.Fee], TestContext.Current.CancellationToken));
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
