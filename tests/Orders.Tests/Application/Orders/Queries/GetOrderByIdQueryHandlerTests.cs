using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Application.Orders.Queries;
using StarterKit.Orders.Api.Domain.OrderTypes;
using StarterKit.Orders.Contracts.Common;
using StarterKit.Shared.Constants;
using StarterKit.Shared.ValueObjects;
using Xunit;

namespace Orders.Tests.Application.Orders.Queries;

public class GetOrderByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var handler = new GetOrderByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new GetOrderByIdQuery(999),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnTheOrder_WithLinesAndFeesAndComputedRollups()
    {
        // Arrange
        using var host = new OrdersTestHost();
        await host.Context.OrderTypes.AddAsync(
            OrderType.Create("SHIPPING", OrderTypeCategory.Fee, "Shipping"),
            TestContext.Current.CancellationToken);
        var order = OrderBuilder.DraftWithLine(unitPrice: 100m, quantity: 2);
        order.AddFee("Shipping", new Money(10m, CurrencyConstants.Default), "SHIPPING", "Shipping");
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetOrderByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new GetOrderByIdQuery(order.Id),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var dto = result.Data!;
        Assert.Equal(order.Id, dto.Id);
        Assert.Equal(order.LocationId, dto.LocationId);
        Assert.Single(dto.Lines);
        var fee = Assert.Single(dto.Fees);
        Assert.Equal("SHIPPING", fee.FeeTypeId);
        Assert.Equal("Shipping", fee.FeeTypeName);
        Assert.Equal(200m, dto.Subtotal);
        Assert.Equal(210m, dto.Total);
    }

    [Fact]
    public async Task Handle_ShouldReturnTheOrderCodeAndExternalReferenceCode_AndSnapshotOntoLinesAndFees()
    {
        // Arrange
        using var host = new OrdersTestHost();
        await host.Context.OrderTypes.AddAsync(
            OrderType.Create("SHIPPING", OrderTypeCategory.Fee, "Shipping"),
            TestContext.Current.CancellationToken);
        var order = OrderBuilder.Draft(orderCode: "ORDER-CODE-XYZ", externalReferenceCode: "EXT-REF-1");
        OrderBuilder.AddLine(order);
        order.AddFee("Shipping", new Money(10m, CurrencyConstants.Default), "SHIPPING", "Shipping");
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetOrderByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new GetOrderByIdQuery(order.Id),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var dto = result.Data!;
        Assert.Equal("ORDER-CODE-XYZ", dto.OrderCode);
        Assert.Equal("EXT-REF-1", dto.ExternalReferenceCode);
        Assert.Equal("ORDER-CODE-XYZ", Assert.Single(dto.Lines).OrderCode);
        Assert.Equal("ORDER-CODE-XYZ", Assert.Single(dto.Fees).OrderCode);
    }
}
