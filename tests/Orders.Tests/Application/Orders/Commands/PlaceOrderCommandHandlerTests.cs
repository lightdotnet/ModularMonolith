using Light.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Orders.Tests.TestSupport;
using StarterKit.Inventory.Contracts.Services;
using StarterKit.Inventory.Contracts.Stock;
using StarterKit.Orders.Api.Application.Orders.Commands;
using StarterKit.Orders.Api.Domain.Orders;
using StarterKit.Orders.Contracts.Common;
using StarterKit.Orders.Contracts.Events;
using Xunit;

namespace Orders.Tests.Application.Orders.Commands;

public class PlaceOrderCommandHandlerTests
{
    private static PlaceOrderCommandHandler MakeHandler(OrdersTestHost host, Mock<IInventoryService> inventoryServiceMock) =>
        new(host.Context, inventoryServiceMock.Object, host.DateTime, host.Publisher, NullLogger<PlaceOrderCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var handler = MakeHandler(host, new Mock<IInventoryService>());

        // Act
        var result = await handler.Handle(
            new PlaceOrderCommand(999, "user-1"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenOrderHasNoLines()
    {
        // Arrange — Order.Place's own guard throws directly; this handler has no try/catch
        // translation (unlike e.g. ApprovalService), so the domain exception propagates as-is.
        using var host = new OrdersTestHost();
        var order = OrderBuilder.Draft();
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = MakeHandler(host, new Mock<IInventoryService>());

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new PlaceOrderCommand(order.Id, "user-1"),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ShouldPlaceTheOrder_AndPublishTheIntegrationAndDomainEvents()
    {
        // Arrange
        using var host = new OrdersTestHost();
        host.DateTime.UtcNow = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var order = OrderBuilder.DraftWithLine(unitPrice: 100m, quantity: 2);
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var inventoryServiceMock = new Mock<IInventoryService>();
        var handler = MakeHandler(host, inventoryServiceMock);

        // Act
        var result = await handler.Handle(
            new PlaceOrderCommand(order.Id, "user-1"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var reloaded = await host.Context.Orders.FirstAsync(x => x.Id == order.Id, TestContext.Current.CancellationToken);
        Assert.Equal(OrderStatus.Placed, reloaded.Status);

        inventoryServiceMock.Verify(
            x => x.DecrementForOrderAsync(
                order.Id,
                order.LocationId,
                It.Is<IReadOnlyList<StockLine>>(lines =>
                    lines.Count == 1
                    && lines[0].ProductId == order.Lines[0].ProductId
                    && lines[0].OrderLineId == order.Lines[0].Id
                    && lines[0].Quantity == order.Lines[0].Quantity),
                "user-1",
                It.IsAny<CancellationToken>()),
            Times.Once);

        var integrationEvent = Assert.Single(host.Publisher.OfType<OrderPlacedIntegrationEvent>());
        Assert.Equal(order.Id, integrationEvent.OrderId);
        Assert.Equal(order.LocationId, integrationEvent.LocationId);
        Assert.Equal(host.DateTime.UtcNow, integrationEvent.PlacedAt);
        var lineSnapshot = Assert.Single(integrationEvent.Lines);
        Assert.Equal(order.Lines[0].ProductId, lineSnapshot.ProductId);
        Assert.Equal(order.Lines[0].Sku, lineSnapshot.Sku);
        Assert.Equal(order.Lines[0].Quantity, lineSnapshot.Quantity);
        Assert.Equal(order.Lines[0].UnitPrice.Amount, lineSnapshot.UnitPrice);
        Assert.Equal(order.Lines[0].RequestedSalePrice?.Amount, lineSnapshot.RequestedSalePrice);

        Assert.Single(host.Publisher.OfType<OrderPlacedEvent>());
    }

    [Fact]
    public async Task Handle_ShouldStillSucceed_WhenPublishingTheIntegrationEventThrows()
    {
        // Arrange — a downstream subscriber fault must never fail the placement call.
        using var host = new OrdersTestHost();
        var order = OrderBuilder.DraftWithLine();
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        host.Publisher.ThrowFor = n => n is OrderPlacedIntegrationEvent;
        var handler = MakeHandler(host, new Mock<IInventoryService>());

        // Act
        var result = await handler.Handle(
            new PlaceOrderCommand(order.Id, "user-1"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var reloaded = await host.Context.Orders.FirstAsync(x => x.Id == order.Id, TestContext.Current.CancellationToken);
        Assert.Equal(OrderStatus.Placed, reloaded.Status);
    }

    [Fact]
    public async Task Handle_ShouldNotPersistThePlacement_WhenInventoryThrowsConflict()
    {
        // Arrange — an oversell throws before SaveChangesAsync is ever called, so the order stays
        // Draft in the database even though Place() already ran on the in-memory tracked instance.
        using var host = new OrdersTestHost();
        var order = OrderBuilder.DraftWithLine();
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var inventoryServiceMock = new Mock<IInventoryService>();
        inventoryServiceMock
            .Setup(x => x.DecrementForOrderAsync(
                order.Id,
                order.LocationId,
                It.IsAny<IReadOnlyList<StockLine>>(),
                "user-1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("Insufficient stock"));
        var handler = MakeHandler(host, inventoryServiceMock);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new PlaceOrderCommand(order.Id, "user-1"),
            TestContext.Current.CancellationToken));

        var reloaded = await host.Context.Orders
            .AsNoTracking()
            .FirstAsync(x => x.Id == order.Id, TestContext.Current.CancellationToken);
        Assert.Equal(OrderStatus.Draft, reloaded.Status);
        inventoryServiceMock.Verify(
            x => x.RestoreForOrderAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<DateTimeOffset?>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCompensateStockAndRethrow_WhenSaveChangesAsyncFails()
    {
        // Arrange — force the final SaveChangesAsync (after the decrement already succeeded) to
        // throw by disposing the shared Sqlite connection from inside the decrement callback, i.e.
        // exactly between the successful decrement and the local commit. Mirrors
        // LeaveManagement.Tests' equivalent compensation-on-final-save-failure tests.
        using var host = new OrdersTestHost();
        var order = OrderBuilder.DraftWithLine();
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var inventoryServiceMock = new Mock<IInventoryService>();
        inventoryServiceMock
            .Setup(x => x.DecrementForOrderAsync(
                order.Id,
                order.LocationId,
                It.IsAny<IReadOnlyList<StockLine>>(),
                "user-1",
                It.IsAny<CancellationToken>()))
            .Callback(() => host.Context.Database.GetDbConnection().Dispose())
            .ReturnsAsync([]);
        var handler = MakeHandler(host, inventoryServiceMock);

        // Act & Assert — the original save failure must still surface to the caller.
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(
            new PlaceOrderCommand(order.Id, "user-1"),
            TestContext.Current.CancellationToken));

        inventoryServiceMock.Verify(
            x => x.RestoreForOrderAsync(order.Id, "user-1", It.IsAny<CancellationToken>(), It.IsAny<DateTimeOffset?>()),
            Times.Once);
    }
}
