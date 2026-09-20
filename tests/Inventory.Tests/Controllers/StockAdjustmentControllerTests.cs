using Inventory.Tests.TestSupport;
using Light.Contracts;
using Light.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StarterKit.Inventory.Api.Application.StockAdjustments.Commands;
using StarterKit.Inventory.Api.Application.StockAdjustments.Queries;
using StarterKit.Inventory.Api.Controllers;
using StarterKit.Inventory.Contracts.Stock;
using StarterKit.Shared;
using Xunit;

namespace Inventory.Tests.Controllers;

/// <summary>
/// Only the mediator-dispatch wiring is unit-tested here — the class-level
/// `[MustHavePermission]` gate itself is ASP.NET Core authorization middleware behavior, not
/// something a plain controller-instantiation test exercises. Mirrors
/// <c>Orders.Tests.Controllers.OrderControllerTests</c>.
/// </summary>
public class StockAdjustmentControllerTests
{
    private const string CurrentUserId = "current-user";

    private static (StockAdjustmentController Controller, Mock<IMediator> Mediator) CreateSut()
    {
        var mediatorMock = new Mock<IMediator>();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton(mediatorMock.Object).BuildServiceProvider(),
        };
        var controller = new StockAdjustmentController(new FakeCurrentUser { UserId = CurrentUserId })
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
        return (controller, mediatorMock);
    }

    [Fact]
    public async Task SearchAsync_ShouldDispatchQuery()
    {
        var (controller, mediatorMock) = CreateSut();
        var request = new SearchStockAdjustmentRequest { LocationId = "location-1" };
        var expected = new PagedResult<StockAdjustmentDto>([], 1, 20, 0);
        mediatorMock
            .Setup(m => m.Send(It.Is<SearchStockAdjustmentsQuery>(q => q.Request == request), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var response = await controller.SearchAsync(request);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task PostAsync_ShouldDispatchCommand_AsTheCurrentUser()
    {
        var (controller, mediatorMock) = CreateSut();
        var request = new RecordStockMovementRequest { ProductId = 1, LocationId = "location-1", QuantityDelta = 5 };
        var expected = Result.Success();
        mediatorMock
            .Setup(m => m.Send(
                It.Is<RecordStockMovementCommand>(c => c.Model == request && c.PerformedByUserId == CurrentUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var response = await controller.PostAsync(request);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }
}
