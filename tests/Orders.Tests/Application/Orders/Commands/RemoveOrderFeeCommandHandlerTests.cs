using Microsoft.EntityFrameworkCore;
using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Application.Orders.Commands;
using StarterKit.Orders.Api.Domain.OrderTypes;
using StarterKit.Orders.Contracts.Common;
using StarterKit.Shared.Constants;
using StarterKit.Shared.ValueObjects;
using Xunit;

namespace Orders.Tests.Application.Orders.Commands;

public class RemoveOrderFeeCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var handler = new RemoveOrderFeeCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new RemoveOrderFeeCommand(999, 999),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldRemoveTheFee()
    {
        // Arrange
        using var host = new OrdersTestHost();
        await host.Context.OrderTypes.AddAsync(
            OrderType.Create("SHIPPING", OrderTypeCategory.Fee, "Shipping"),
            TestContext.Current.CancellationToken);
        var order = OrderBuilder.Draft();
        order.AddFee("Shipping", new Money(10m, CurrencyConstants.Default), "SHIPPING", "Shipping");
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var feeId = order.Fees[0].Id;
        var handler = new RemoveOrderFeeCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new RemoveOrderFeeCommand(order.Id, feeId),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var reloaded = await host.Context.Orders
            .Include(x => x.Fees)
            .FirstAsync(x => x.Id == order.Id, TestContext.Current.CancellationToken);
        Assert.Empty(reloaded.Fees);
    }
}
