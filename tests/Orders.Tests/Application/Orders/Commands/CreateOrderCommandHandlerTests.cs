using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Orders.Tests.TestSupport;
using StarterKit.Locations.Contracts.Services;
using StarterKit.Orders.Api.Application.Orders.Commands;
using StarterKit.Orders.Contracts.Common;
using StarterKit.Orders.Contracts.Orders;
using Xunit;

namespace Orders.Tests.Application.Orders.Commands;

public class CreateOrderCommandHandlerTests
{
    private static CreateOrderCommandHandler MakeHandler(OrdersTestHost host, Mock<ILocationDirectoryService> locationServiceMock) =>
        new(
            host.Context,
            locationServiceMock.Object,
            CurrencyServiceMock.Create().Object,
            host.DateTime,
            NullLogger<CreateOrderCommandHandler>.Instance);

    private static Mock<ILocationDirectoryService> MakeLocationServiceMock(bool locationExists = true)
    {
        var mock = new Mock<ILocationDirectoryService>();
        mock.Setup(s => s.ExistsAsync("location-1", It.IsAny<CancellationToken>())).ReturnsAsync(locationExists);
        return mock;
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenLocationDoesNotExist()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var locationServiceMock = MakeLocationServiceMock(locationExists: false);
        var handler = MakeHandler(host, locationServiceMock);

        // Act
        var result = await handler.Handle(
            new CreateOrderCommand(new CreateOrderRequest { LocationId = "location-1" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Empty(host.Context.Orders);
    }

    [Fact]
    public async Task Handle_ShouldCreateDraftOrder_WithAGeneratedOrderCode_WhenNoneIsSupplied()
    {
        // Arrange
        using var host = new OrdersTestHost();
        host.DateTime.UtcNow = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var locationServiceMock = MakeLocationServiceMock();
        var handler = MakeHandler(host, locationServiceMock);

        // Act
        var result = await handler.Handle(
            new CreateOrderCommand(new CreateOrderRequest { LocationId = "location-1", MemberId = "member-1" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var entity = Assert.Single(host.Context.Orders);
        Assert.Equal(result.Data, entity.Id);
        Assert.Equal("location-1", entity.LocationId);
        Assert.Equal("member-1", entity.MemberId);
        Assert.Equal(OrderStatus.Draft, entity.Status);
        Assert.Equal(17, entity.OrderCode.Value.Length);
        Assert.StartsWith("20260101", entity.OrderCode.Value);
        Assert.Null(entity.ExternalReferenceCode);
    }

    [Fact]
    public async Task Handle_ShouldCreateOrder_WithTheCallerSuppliedOrderCode_WhenProvidedAndUnique()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var locationServiceMock = MakeLocationServiceMock();
        var handler = MakeHandler(host, locationServiceMock);

        // Act
        var result = await handler.Handle(
            new CreateOrderCommand(new CreateOrderRequest
            {
                LocationId = "location-1",
                OrderCode = "MY-CUSTOM-CODE",
                ExternalReferenceCode = "POS-REF-1",
            }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var entity = Assert.Single(host.Context.Orders);
        Assert.Equal("MY-CUSTOM-CODE", entity.OrderCode.Value);
        Assert.Equal("POS-REF-1", entity.ExternalReferenceCode);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_AndNotCreateAnOrder_WhenTheCallerSuppliedOrderCodeIsAlreadyTaken()
    {
        // Arrange — the pre-check-then-friendly-conflict path: no silent regeneration for a
        // caller-supplied code, unlike the generated path's own bounded retry loop.
        using var host = new OrdersTestHost();
        var existing = OrderBuilder.Draft(orderCode: "TAKEN-CODE");
        await host.Context.Orders.AddAsync(existing, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var locationServiceMock = MakeLocationServiceMock();
        var handler = MakeHandler(host, locationServiceMock);

        // Act
        var result = await handler.Handle(
            new CreateOrderCommand(new CreateOrderRequest { LocationId = "location-1", OrderCode = "TAKEN-CODE" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        var only = Assert.Single(host.Context.Orders);
        Assert.Equal(existing.Id, only.Id);
        Assert.Equal("TAKEN-CODE", only.OrderCode.Value);
    }
}
