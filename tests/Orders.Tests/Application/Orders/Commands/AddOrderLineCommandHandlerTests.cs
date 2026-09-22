using Microsoft.EntityFrameworkCore;
using Moq;
using Orders.Tests.TestSupport;
using StarterKit.Catalog.Contracts.Common;
using StarterKit.Catalog.Contracts.Products;
using StarterKit.Catalog.Contracts.Services;
using StarterKit.Orders.Api.Application.Orders.Commands;
using StarterKit.Orders.Contracts.Orders;
using StarterKit.Shared.Constants;
using Xunit;

namespace Orders.Tests.Application.Orders.Commands;

public class AddOrderLineCommandHandlerTests
{
    private static ProductPriceInfoDto MakePriceInfo(ProductStatus status = ProductStatus.Active) => new()
    {
        Id = 1,
        ProductName = "Widget",
        Sku = "SKU-1",
        Price = 100m,
        Currency = CurrencyConstants.Default,
        VatRate = 10m,
        Status = status,
    };

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var pricingMock = new Mock<ICatalogPricingService>();
        var handler = new AddOrderLineCommandHandler(
            host.Context,
            pricingMock.Object,
            CurrencyServiceMock.Create().Object,
            host.DateTime);

        // Act
        var result = await handler.Handle(
            new AddOrderLineCommand(999, new AddOrderLineRequest { ProductId = 1, Quantity = 1 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var order = OrderBuilder.Draft();
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var pricingMock = new Mock<ICatalogPricingService>();
        pricingMock
            .Setup(s => s.GetPriceInfoAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductPriceInfoDto?)null);
        var handler = new AddOrderLineCommandHandler(
            host.Context,
            pricingMock.Object,
            CurrencyServiceMock.Create().Object,
            host.DateTime);

        // Act
        var result = await handler.Handle(
            new AddOrderLineCommand(order.Id, new AddOrderLineRequest { ProductId = 1, Quantity = 1 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenProductIsNotActive()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var order = OrderBuilder.Draft();
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var pricingMock = new Mock<ICatalogPricingService>();
        pricingMock
            .Setup(s => s.GetPriceInfoAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakePriceInfo(ProductStatus.Inactive));
        var handler = new AddOrderLineCommandHandler(
            host.Context,
            pricingMock.Object,
            CurrencyServiceMock.Create().Object,
            host.DateTime);

        // Act
        var result = await handler.Handle(
            new AddOrderLineCommand(order.Id, new AddOrderLineRequest { ProductId = 1, Quantity = 1 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldAddLine_ResolvingPriceVatNameAndSku_FromCatalogPricing()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var order = OrderBuilder.Draft();
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var pricingMock = new Mock<ICatalogPricingService>();
        pricingMock
            .Setup(s => s.GetPriceInfoAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakePriceInfo());
        var handler = new AddOrderLineCommandHandler(
            host.Context,
            pricingMock.Object,
            CurrencyServiceMock.Create().Object,
            host.DateTime);

        // Act
        var result = await handler.Handle(
            new AddOrderLineCommand(
                order.Id,
                new AddOrderLineRequest { ProductId = 1, Quantity = 2, RequestedSalePrice = 80m }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var reloaded = await host.Context.Orders
            .Include(x => x.Lines)
            .FirstAsync(x => x.Id == order.Id, TestContext.Current.CancellationToken);
        var line = Assert.Single(reloaded.Lines);
        Assert.Equal(1, line.ProductId);
        Assert.Equal("Widget", line.ProductName);
        Assert.Equal("SKU-1", line.Sku);
        Assert.Equal(100m, line.UnitPrice.Amount);
        Assert.Equal(10m, line.VatRate.Value);
        Assert.Equal(2, line.Quantity);
        Assert.Equal(80m, line.RequestedSalePrice?.Amount);
    }
}
