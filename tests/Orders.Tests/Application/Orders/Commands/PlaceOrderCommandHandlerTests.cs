using Light.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Application.Orders.Commands;
using StarterKit.Orders.Api.Domain.Orders;
using StarterKit.Orders.Contracts.Common;
using StarterKit.Orders.Contracts.Events;
using Xunit;

namespace Orders.Tests.Application.Orders.Commands;

public class PlaceOrderCommandHandlerTests
{
    private static PlaceOrderCommandHandler MakeHandler(OrdersTestHost host) =>
        new(host.Context, host.DateTime, host.Publisher, NullLogger<PlaceOrderCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var handler = MakeHandler(host);

        // Act
        var result = await handler.Handle(
            new PlaceOrderCommand(999),
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
        var handler = MakeHandler(host);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new PlaceOrderCommand(order.Id),
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
        var handler = MakeHandler(host);

        // Act
        var result = await handler.Handle(
            new PlaceOrderCommand(order.Id),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var reloaded = await host.Context.Orders.FirstAsync(x => x.Id == order.Id, TestContext.Current.CancellationToken);
        Assert.Equal(OrderStatus.Placed, reloaded.Status);

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
        var handler = MakeHandler(host);

        // Act
        var result = await handler.Handle(
            new PlaceOrderCommand(order.Id),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var reloaded = await host.Context.Orders.FirstAsync(x => x.Id == order.Id, TestContext.Current.CancellationToken);
        Assert.Equal(OrderStatus.Placed, reloaded.Status);
    }
}
