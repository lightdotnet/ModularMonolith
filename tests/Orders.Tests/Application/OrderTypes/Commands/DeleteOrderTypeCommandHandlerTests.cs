using Moq;
using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Application.OrderTypes.Commands;
using StarterKit.Orders.Api.Domain.OrderTypes;
using StarterKit.Orders.Api.Services;
using StarterKit.Orders.Contracts.Common;
using StarterKit.Shared.Constants;
using StarterKit.Shared.ValueObjects;
using Xunit;

namespace Orders.Tests.Application.OrderTypes.Commands;

public class DeleteOrderTypeCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenTypeDoesNotExist()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var cacheMock = new Mock<IOrderTypeCache>();
        var handler = new DeleteOrderTypeCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new DeleteOrderTypeCommand("missing", OrderTypeCategory.Fee),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        cacheMock.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Proves the lookup is keyed by the full composite <c>(Id, Category)</c> pair — a matching Id
    /// under a different category must not be found (and therefore not deleted).
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenIdExistsOnlyUnderADifferentCategory()
    {
        // Arrange
        using var host = new OrdersTestHost();
        await SeedTypeAsync(host, "OTHER", OrderTypeCategory.Fee);
        var cacheMock = new Mock<IOrderTypeCache>();
        var handler = new DeleteOrderTypeCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new DeleteOrderTypeCommand("OTHER", OrderTypeCategory.Payment),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(await host.Context.OrderTypes.FindAsync(["OTHER", OrderTypeCategory.Fee], TestContext.Current.CancellationToken));
        cacheMock.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// <c>OrderFee</c>/<c>Payment</c> rows keep a denormalized <c>*TypeName</c> snapshot, so they no
    /// longer depend on the catalog row existing — deletion is allowed even when historically
    /// referenced. No in-use guard is reintroduced, mirroring <c>DeleteFeeType</c>/
    /// <c>DeletePaymentType</c> today.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldDelete_AndReloadCache_WhenReferencedByOrderFee()
    {
        // Arrange
        using var host = new OrdersTestHost();
        await SeedTypeAsync(host, "SHIPPING", OrderTypeCategory.Fee);
        var order = OrderBuilder.Draft();
        order.AddFee("Shipping", new Money(10m, CurrencyConstants.Default), "SHIPPING", "Shipping");
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var cacheMock = new Mock<IOrderTypeCache>();
        var handler = new DeleteOrderTypeCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new DeleteOrderTypeCommand("SHIPPING", OrderTypeCategory.Fee),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(await host.Context.OrderTypes.FindAsync(["SHIPPING", OrderTypeCategory.Fee], TestContext.Current.CancellationToken));
        cacheMock.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldDelete_AndReloadCache_WhenReferencedByPayment()
    {
        // Arrange
        using var host = new OrdersTestHost();
        await SeedTypeAsync(host, "CASH", OrderTypeCategory.Payment);
        var order = OrderBuilder.Placed(host.DateTime.UtcNow, unitPrice: 100m, quantity: 1);
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var payment = PaymentBuilder.Build(order.Id, 50m, host.DateTime.UtcNow, "CASH", orderCode: order.OrderCode.Value);
        await host.Context.Payments.AddAsync(payment, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var cacheMock = new Mock<IOrderTypeCache>();
        var handler = new DeleteOrderTypeCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new DeleteOrderTypeCommand("CASH", OrderTypeCategory.Payment),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(await host.Context.OrderTypes.FindAsync(["CASH", OrderTypeCategory.Payment], TestContext.Current.CancellationToken));
        cacheMock.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldDelete_AndReloadCache_WhenNotInUse()
    {
        // Arrange
        using var host = new OrdersTestHost();
        await SeedTypeAsync(host, "SHIPPING", OrderTypeCategory.Fee);
        var cacheMock = new Mock<IOrderTypeCache>();
        var handler = new DeleteOrderTypeCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new DeleteOrderTypeCommand("SHIPPING", OrderTypeCategory.Fee),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(await host.Context.OrderTypes.FindAsync(["SHIPPING", OrderTypeCategory.Fee], TestContext.Current.CancellationToken));
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
