using Light.Extensions.Caching;
using Microsoft.EntityFrameworkCore;
using Moq;
using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Application.Orders.Commands;
using StarterKit.Orders.Api.Domain.OrderTypes;
using StarterKit.Orders.Api.Services;
using StarterKit.Orders.Contracts.Common;
using StarterKit.Orders.Contracts.Orders;
using Xunit;

namespace Orders.Tests.Application.Orders.Commands;

public class AddOrderFeeCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var cacheMock = await CacheReturningAsync(host, "SHIPPING");
        var handler = new AddOrderFeeCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new AddOrderFeeCommand(
                999,
                new AddOrderFeeRequest { Name = "Shipping", Amount = 10m, FeeTypeId = "SHIPPING" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenFeeTypeDoesNotExist()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var order = OrderBuilder.Draft();
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var cacheMock = new Mock<IOrderTypeCache>();
        cacheMock
            .Setup(x => x.GetAsync("MISSING", OrderTypeCategory.Fee, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrderType?)null);
        var handler = new AddOrderFeeCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new AddOrderFeeCommand(
                order.Id,
                new AddOrderFeeRequest { Name = "Shipping", Amount = 10m, FeeTypeId = "MISSING" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenFeeTypeIsInactive()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var order = OrderBuilder.Draft();
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var inactiveType = OrderType.Create("SHIPPING", OrderTypeCategory.Fee, "Shipping");
        inactiveType.Update("Shipping", OrderTypeStatus.Inactive);
        await host.Context.OrderTypes.AddAsync(inactiveType, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var cacheMock = new Mock<IOrderTypeCache>();
        cacheMock
            .Setup(x => x.GetAsync("SHIPPING", OrderTypeCategory.Fee, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveType);
        var handler = new AddOrderFeeCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new AddOrderFeeCommand(
                order.Id,
                new AddOrderFeeRequest { Name = "Shipping", Amount = 10m, FeeTypeId = "SHIPPING" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    /// <summary>
    /// A Payment-category type must not be usable as a fee type. Uses the real <see cref="OrderTypeCache"/>
    /// (not a mock) seeded with an "OTHER" row under <see cref="OrderTypeCategory.Payment"/> only, so
    /// this exercises the actual composite-key guard end to end: the handler looks the id up under
    /// <see cref="OrderTypeCategory.Fee"/>, which genuinely finds nothing.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenIdBelongsToAPaymentCategoryType()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var order = OrderBuilder.Draft();
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await host.Context.OrderTypes.AddAsync(
            OrderType.Create("OTHER", OrderTypeCategory.Payment, "Other"),
            TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var cache = new OrderTypeCache(CreateStatefulCacheMock(), host.Context);
        var handler = new AddOrderFeeCommandHandler(host.Context, cache);

        // Act
        var result = await handler.Handle(
            new AddOrderFeeCommand(
                order.Id,
                new AddOrderFeeRequest { Name = "Other", Amount = 10m, FeeTypeId = "OTHER" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldAddTheFee()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var order = OrderBuilder.Draft();
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var cacheMock = await CacheReturningAsync(host, "SHIPPING");
        var handler = new AddOrderFeeCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new AddOrderFeeCommand(
                order.Id,
                new AddOrderFeeRequest { Name = "Shipping", Amount = 15m, FeeTypeId = "SHIPPING" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var reloaded = await host.Context.Orders
            .Include(x => x.Fees)
            .FirstAsync(x => x.Id == order.Id, TestContext.Current.CancellationToken);
        var fee = Assert.Single(reloaded.Fees);
        Assert.Equal("Shipping", fee.Name);
        Assert.Equal(15m, fee.Amount.Amount);
        Assert.Equal("SHIPPING", fee.FeeTypeId);
        Assert.Equal("SHIPPING", fee.FeeTypeName);
    }

    /// <summary>
    /// Seeds a real <see cref="OrderType"/> row (the FK the persisted <c>OrderFee</c> needs to
    /// satisfy the database constraint) and mocks the cache lookup the handler uses to validate it up
    /// front — mirrors <c>Location.Tests</c>' <c>CreateLocationCommandHandlerTests.CacheReturning</c>.
    /// </summary>
    private static async Task<Mock<IOrderTypeCache>> CacheReturningAsync(OrdersTestHost host, string feeTypeId)
    {
        var type = OrderType.Create(feeTypeId, OrderTypeCategory.Fee, feeTypeId);
        await host.Context.OrderTypes.AddAsync(type, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mock = new Mock<IOrderTypeCache>();
        mock
            .Setup(x => x.GetAsync(feeTypeId, OrderTypeCategory.Fee, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrderType.Create(feeTypeId, OrderTypeCategory.Fee, feeTypeId));
        return mock;
    }

    /// <summary>
    /// Stateful <see cref="ICacheService"/> mock (backed by a local field standing in for whatever the
    /// real distributed/memory cache implementation would store) so a real <see cref="OrderTypeCache"/>
    /// can be exercised against it — see <see cref="Handle_ShouldReturnNotFound_WhenIdBelongsToAPaymentCategoryType"/>.
    /// Mirrors <c>Services.OrderTypeCacheTests.CreateStatefulCacheMock</c>.
    /// </summary>
    private static ICacheService CreateStatefulCacheMock()
    {
        List<OrderType>? stored = null;

        var mock = new Mock<ICacheService>();

        mock.Setup(x => x.GetAsync<List<OrderType>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => stored!);

        mock.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<List<OrderType>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, List<OrderType>, TimeSpan?, CancellationToken>((_, value, _, _) => stored = value)
            .Returns(Task.CompletedTask);

        return mock.Object;
    }
}
