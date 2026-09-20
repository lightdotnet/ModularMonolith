using Light.Contracts;
using Light.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StarterKit.Inventory.Api.Application.StockLevels.Queries;
using StarterKit.Inventory.Api.Controllers;
using StarterKit.Inventory.Contracts.Stock;
using Xunit;

namespace Inventory.Tests.Controllers;

/// <summary>
/// Only the mediator-dispatch wiring is unit-tested here — the class-level
/// `[MustHavePermission]` gate itself is ASP.NET Core authorization middleware behavior, not
/// something a plain controller-instantiation test exercises. Mirrors
/// <c>Orders.Tests.Controllers.OrderControllerTests</c>.
/// </summary>
public class StockLevelControllerTests
{
    private static (StockLevelController Controller, Mock<IMediator> Mediator) CreateSut()
    {
        var mediatorMock = new Mock<IMediator>();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton(mediatorMock.Object).BuildServiceProvider(),
        };
        var controller = new StockLevelController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
        return (controller, mediatorMock);
    }

    [Fact]
    public async Task SearchAsync_ShouldDispatchQuery()
    {
        var (controller, mediatorMock) = CreateSut();
        var request = new SearchStockLevelRequest { LocationId = "location-1" };
        var expected = new PagedResult<StockLevelDto>([], 1, 20, 0);
        mediatorMock
            .Setup(m => m.Send(It.Is<SearchStockLevelsQuery>(q => q.Request == request), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var response = await controller.SearchAsync(request);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task GetTotalAsync_ShouldDispatchQuery()
    {
        var (controller, mediatorMock) = CreateSut();
        var expected = Result<ProductStockTotalDto>.Success(new ProductStockTotalDto { ProductId = 42 });
        mediatorMock
            .Setup(m => m.Send(It.Is<GetProductStockTotalQuery>(q => q.ProductId == 42), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var response = await controller.GetTotalAsync(42);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }
}
