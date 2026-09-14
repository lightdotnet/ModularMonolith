using Light.Contracts;
using Light.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Application.Orders.Commands;
using StarterKit.Orders.Api.Application.Orders.Queries;
using StarterKit.Orders.Api.Controllers;
using StarterKit.Orders.Contracts.Common;
using StarterKit.Orders.Contracts.Orders;
using StarterKit.Shared;
using Xunit;

namespace Orders.Tests.Controllers;

/// <summary>
/// Only the mediator-dispatch wiring is unit-tested here — the class-level
/// `[MustHavePermission]` gate itself is ASP.NET Core authorization middleware behavior, not
/// something a plain controller-instantiation test exercises. Mirrors
/// <c>Approval.Tests.Controllers.ApprovalControllerTests</c>.
/// </summary>
public class OrderControllerTests
{
    private const string CurrentUserId = "current-user";

    private static (OrderController Controller, Mock<IMediator> Mediator) CreateSut()
    {
        var mediatorMock = new Mock<IMediator>();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton(mediatorMock.Object).BuildServiceProvider(),
        };
        var controller = new OrderController(new FakeCurrentUser { UserId = CurrentUserId })
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
        return (controller, mediatorMock);
    }

    [Fact]
    public async Task SearchAsync_ShouldDispatchQuery()
    {
        var (controller, mediatorMock) = CreateSut();
        var request = new SearchOrderRequest { LocationId = "location-1" };
        var expected = new PagedResult<OrderDto>([], 1, 20, 0);
        mediatorMock
            .Setup(m => m.Send(It.Is<SearchOrdersQuery>(q => q.Request == request), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var response = await controller.SearchAsync(request);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task GetAsync_ShouldDispatchQuery()
    {
        var (controller, mediatorMock) = CreateSut();
        var expected = Result<OrderDto>.Success(new OrderDto { Id = 1 });
        mediatorMock
            .Setup(m => m.Send(It.Is<GetOrderByIdQuery>(q => q.Id == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var response = await controller.GetAsync(1);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task PostAsync_ShouldDispatchCommand()
    {
        var (controller, mediatorMock) = CreateSut();
        var request = new CreateOrderRequest { LocationId = "location-1" };
        var expected = Result<long>.Success(1);
        mediatorMock
            .Setup(m => m.Send(It.Is<CreateOrderCommand>(c => c.Model == request), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var response = await controller.PostAsync(request);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task AddLineAsync_ShouldDispatchCommand()
    {
        var (controller, mediatorMock) = CreateSut();
        var request = new AddOrderLineRequest { ProductId = 10, Quantity = 1 };
        var expected = Result.Success();
        mediatorMock
            .Setup(m => m.Send(
                It.Is<AddOrderLineCommand>(c => c.OrderId == 1 && c.Model == request),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var response = await controller.AddLineAsync(1, request);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task UpdateLineQuantityAsync_ShouldDispatchCommand()
    {
        var (controller, mediatorMock) = CreateSut();
        var request = new UpdateOrderLineQuantityRequest { Quantity = 3 };
        var expected = Result.Success();
        mediatorMock
            .Setup(m => m.Send(
                It.Is<UpdateOrderLineQuantityCommand>(c => c.OrderId == 1 && c.OrderLineId == 2 && c.Model == request),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var response = await controller.UpdateLineQuantityAsync(1, 2, request);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task SetLineSalePriceAsync_ShouldDispatchCommand()
    {
        var (controller, mediatorMock) = CreateSut();
        var request = new SetOrderLineSalePriceRequest { SalePrice = 50m };
        var expected = Result.Success();
        mediatorMock
            .Setup(m => m.Send(
                It.Is<SetOrderLineSalePriceCommand>(c => c.OrderId == 1 && c.OrderLineId == 2 && c.Model == request),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var response = await controller.SetLineSalePriceAsync(1, 2, request);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task RemoveLineAsync_ShouldDispatchCommand()
    {
        var (controller, mediatorMock) = CreateSut();
        var expected = Result.Success();
        mediatorMock
            .Setup(m => m.Send(
                It.Is<RemoveOrderLineCommand>(c => c.OrderId == 1 && c.OrderLineId == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var response = await controller.RemoveLineAsync(1, 2);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task ApplyDiscountAsync_ShouldDispatchCommand()
    {
        var (controller, mediatorMock) = CreateSut();
        var request = new ApplyOrderDiscountRequest { Kind = OrderDiscountKind.FixedAmount, Value = 10m };
        var expected = Result.Success();
        mediatorMock
            .Setup(m => m.Send(
                It.Is<ApplyOrderDiscountCommand>(c => c.OrderId == 1 && c.Model == request),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var response = await controller.ApplyDiscountAsync(1, request);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task RemoveDiscountAsync_ShouldDispatchCommand()
    {
        var (controller, mediatorMock) = CreateSut();
        var expected = Result.Success();
        mediatorMock
            .Setup(m => m.Send(It.Is<RemoveOrderDiscountCommand>(c => c.OrderId == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var response = await controller.RemoveDiscountAsync(1);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task AddFeeAsync_ShouldDispatchCommand()
    {
        var (controller, mediatorMock) = CreateSut();
        var request = new AddOrderFeeRequest { Name = "Shipping", Amount = 10m, Type = OrderFeeType.Shipping };
        var expected = Result<long>.Success(2);
        mediatorMock
            .Setup(m => m.Send(
                It.Is<AddOrderFeeCommand>(c => c.OrderId == 1 && c.Model == request),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var response = await controller.AddFeeAsync(1, request);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task RemoveFeeAsync_ShouldDispatchCommand()
    {
        var (controller, mediatorMock) = CreateSut();
        var expected = Result.Success();
        mediatorMock
            .Setup(m => m.Send(
                It.Is<RemoveOrderFeeCommand>(c => c.OrderId == 1 && c.OrderFeeId == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var response = await controller.RemoveFeeAsync(1, 2);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task PlaceAsync_ShouldDispatchCommand()
    {
        var (controller, mediatorMock) = CreateSut();
        var expected = Result.Success();
        mediatorMock
            .Setup(m => m.Send(It.Is<PlaceOrderCommand>(c => c.Id == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var response = await controller.PlaceAsync(1);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task CancelAsync_ShouldDispatchCommand_AsTheCurrentUser()
    {
        var (controller, mediatorMock) = CreateSut();
        var request = new CancelOrderRequest { Reason = "customer request" };
        var expected = Result.Success();
        mediatorMock
            .Setup(m => m.Send(
                It.Is<CancelOrderCommand>(c => c.Id == 1 && c.Model == request && c.CancelledByUserId == CurrentUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var response = await controller.CancelAsync(1, request);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task MarkFulfilledAsync_ShouldDispatchCommand()
    {
        var (controller, mediatorMock) = CreateSut();
        var expected = Result.Success();
        mediatorMock
            .Setup(m => m.Send(It.Is<MarkOrderFulfilledCommand>(c => c.Id == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var response = await controller.MarkFulfilledAsync(1);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }
}
