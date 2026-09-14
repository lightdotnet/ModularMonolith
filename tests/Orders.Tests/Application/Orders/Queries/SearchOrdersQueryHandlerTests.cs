using System.Linq;
using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Application.Orders.Queries;
using StarterKit.Orders.Contracts.Common;
using StarterKit.Orders.Contracts.Orders;
using Xunit;

namespace Orders.Tests.Application.Orders.Queries;

public class SearchOrdersQueryHandlerTests
{
    private static async Task SeedAsync(OrdersTestHost host, StarterKit.Orders.Api.Domain.Orders.Order order)
    {
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldExcludeDraftOrders_ByDefault()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var draft = OrderBuilder.Draft();
        var placed = OrderBuilder.Placed(host.DateTime.UtcNow);
        await SeedAsync(host, draft);
        await SeedAsync(host, placed);
        var handler = new SearchOrdersQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchOrdersQuery(new SearchOrderRequest { PageNumber = 1, PageSize = 20 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.Data.TotalRecords);
        Assert.Equal(placed.Id, Assert.Single(result.Data.Records).Id);
    }

    [Fact]
    public async Task Handle_ShouldIncludeDraftOrders_WhenExplicitlyFilteredFor()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var draft = OrderBuilder.Draft();
        var placed = OrderBuilder.Placed(host.DateTime.UtcNow);
        await SeedAsync(host, draft);
        await SeedAsync(host, placed);
        var handler = new SearchOrdersQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchOrdersQuery(new SearchOrderRequest { Status = OrderStatus.Draft, PageNumber = 1, PageSize = 20 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.Data.TotalRecords);
        Assert.Equal(draft.Id, Assert.Single(result.Data.Records).Id);
    }

    [Fact]
    public async Task Handle_ShouldFilterByLocationId()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var placedAtLocationA = OrderBuilder.Placed(host.DateTime.UtcNow, locationId: "location-a");
        var placedAtLocationB = OrderBuilder.Placed(host.DateTime.UtcNow, locationId: "location-b");
        await SeedAsync(host, placedAtLocationA);
        await SeedAsync(host, placedAtLocationB);
        var handler = new SearchOrdersQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchOrdersQuery(new SearchOrderRequest { LocationId = "location-a", PageNumber = 1, PageSize = 20 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.Data.TotalRecords);
        Assert.Equal(placedAtLocationA.Id, Assert.Single(result.Data.Records).Id);
    }

    [Fact]
    public async Task Handle_ShouldReturnPagingShape()
    {
        // Arrange
        using var host = new OrdersTestHost();
        for (var i = 0; i < 3; i++)
            await SeedAsync(host, OrderBuilder.Placed(host.DateTime.UtcNow));
        var handler = new SearchOrdersQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchOrdersQuery(new SearchOrderRequest { PageNumber = 1, PageSize = 2 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.Data.PageNumber);
        Assert.Equal(2, result.Data.PageSize);
        Assert.Equal(3, result.Data.TotalRecords);
        Assert.Equal(2, result.Data.Records.Count());
    }
}
