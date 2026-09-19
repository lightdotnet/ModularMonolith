using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Application.OrderTypes.Queries;
using StarterKit.Orders.Api.Domain.OrderTypes;
using StarterKit.Orders.Contracts.Common;
using Xunit;

namespace Orders.Tests.Application.OrderTypes.Queries;

public class GetOrderTypeByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnType_WhenFound()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var shippingType = OrderType.Create("SHIPPING", OrderTypeCategory.Fee, "Shipping");
        await host.Context.OrderTypes.AddAsync(shippingType, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetOrderTypeByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new GetOrderTypeByIdQuery("SHIPPING", OrderTypeCategory.Fee),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Shipping", result.Data.Name);
        Assert.Equal(OrderTypeCategory.Fee, result.Data.Category);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenTypeDoesNotExist()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var handler = new GetOrderTypeByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new GetOrderTypeByIdQuery("missing", OrderTypeCategory.Fee),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
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
        var otherFeeType = OrderType.Create("OTHER", OrderTypeCategory.Fee, "Other");
        await host.Context.OrderTypes.AddAsync(otherFeeType, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetOrderTypeByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new GetOrderTypeByIdQuery("OTHER", OrderTypeCategory.Payment),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }
}
