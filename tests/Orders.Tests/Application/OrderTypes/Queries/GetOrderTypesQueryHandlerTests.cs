using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Application.OrderTypes.Queries;
using StarterKit.Orders.Api.Domain.OrderTypes;
using StarterKit.Orders.Contracts.Common;
using Xunit;

namespace Orders.Tests.Application.OrderTypes.Queries;

public class GetOrderTypesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenNoneExist()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var handler = new GetOrderTypesQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetOrderTypesQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_ShouldReturnAllTypes_OrderedByName_WhenNoCategoryFilter()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var shippingType = OrderType.Create("SHIPPING", OrderTypeCategory.Fee, "Shipping");
        var cashType = OrderType.Create("CASH", OrderTypeCategory.Payment, "Cash");
        await host.Context.OrderTypes.AddRangeAsync(shippingType, cashType);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetOrderTypesQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetOrderTypesQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(["Cash", "Shipping"], result.Select(x => x.Name));
    }

    [Fact]
    public async Task Handle_ShouldReturnOnlyMatchingCategory_WhenCategoryFilterProvided()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var shippingType = OrderType.Create("SHIPPING", OrderTypeCategory.Fee, "Shipping");
        var otherFeeType = OrderType.Create("OTHER", OrderTypeCategory.Fee, "Other");
        var cashType = OrderType.Create("CASH", OrderTypeCategory.Payment, "Cash");
        await host.Context.OrderTypes.AddRangeAsync(shippingType, otherFeeType, cashType);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetOrderTypesQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new GetOrderTypesQuery(OrderTypeCategory.Fee),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, x => Assert.Equal(OrderTypeCategory.Fee, x.Category));
        Assert.Equal(["Other", "Shipping"], result.Select(x => x.Name));
    }
}
