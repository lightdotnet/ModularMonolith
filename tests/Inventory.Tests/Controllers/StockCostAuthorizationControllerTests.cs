using System.Security.Claims;
using Inventory.Tests.TestSupport;
using Light.Contracts;
using Light.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StarterKit.Inventory.Api.Application.StockAdjustments.Commands;
using StarterKit.Inventory.Api.Application.StockAdjustments.Queries;
using StarterKit.Inventory.Api.Application.StockLevels.Queries;
using StarterKit.Inventory.Api.Controllers;
using StarterKit.Inventory.Contracts.Authorization;
using StarterKit.Inventory.Contracts.Stock;
using StarterKit.Shared;
using StarterKit.Shared.Authorization;
using Xunit;

namespace Inventory.Tests.Controllers;

/// <summary>
/// Covers the cost-related rules the stock controllers apply in code: which callers see cost fields,
/// and that a manual adjustment's unit cost needs only the endpoint's manage gate (revalue is for the
/// revaluation endpoint). As in
/// <see cref="StockAdjustmentControllerTests"/>, the <c>[MustHavePermission]</c> attributes themselves
/// are authorization middleware behavior and are not exercised here.
/// </summary>
public class StockCostAuthorizationControllerTests
{
    private const string CurrentUserId = "current-user";

    private static ClaimsPrincipal Principal(params string[] permissions) =>
        InventoryCostAccessTests.PrincipalWith(null, permissions);

    private static HttpContext CreateHttpContext(
        Mock<IMediator> mediatorMock,
        ClaimsPrincipal user) =>
        new DefaultHttpContext
        {
            User = user,
            RequestServices = new ServiceCollection().AddSingleton(mediatorMock.Object).BuildServiceProvider(),
        };

    private static (StockAdjustmentController Controller, Mock<IMediator> Mediator) CreateAdjustmentSut(ClaimsPrincipal user)
    {
        var mediatorMock = new Mock<IMediator>();
        var controller = new StockAdjustmentController(new FakeCurrentUser { UserId = CurrentUserId })
        {
            ControllerContext = new ControllerContext { HttpContext = CreateHttpContext(mediatorMock, user) },
        };

        return (controller, mediatorMock);
    }

    private static (StockLevelController Controller, Mock<IMediator> Mediator) CreateLevelSut(ClaimsPrincipal user)
    {
        var mediatorMock = new Mock<IMediator>();
        var controller = new StockLevelController
        {
            ControllerContext = new ControllerContext { HttpContext = CreateHttpContext(mediatorMock, user) },
        };

        return (controller, mediatorMock);
    }

    private static RecordStockMovementRequest Movement(decimal? unitCost) =>
        new()
        {
            ProductId = 1,
            LocationId = "location-1",
            QuantityDelta = 5,
            UnitCost = unitCost,
        };

    private static void SetupRecord(Mock<IMediator> mediatorMock) =>
        mediatorMock
            .Setup(m => m.Send(It.IsAny<RecordStockMovementCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

    [Theory]
    [InlineData("5")]
    [InlineData("0")]
    public async Task PostAsync_ShouldDispatch_WhenAnInboundUnitCostIsGivenWithOnlyTheManagePermission(string unitCost)
    {
        // Arrange — Manage alone (enforced by the endpoint attribute) is enough to first-stock a product.
        var (controller, mediatorMock) = CreateAdjustmentSut(Principal(InventoryPermissions.Stock.Manage));
        SetupRecord(mediatorMock);
        var request = Movement(decimal.Parse(unitCost));

        // Act
        var response = await controller.PostAsync(request);

        // Assert
        Assert.IsType<ObjectResult>(response);
        mediatorMock.Verify(
            m => m.Send(
                It.Is<RecordStockMovementCommand>(c => c.Model == request && c.PerformedByUserId == CurrentUserId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PostAsync_ShouldDispatch_WhenAnOutboundMovementCarriesAUnitCost_WithoutAnyExtraPermission()
    {
        // Arrange — the handler ignores the cost on outbound movements, so it needs no extra permission.
        var (controller, mediatorMock) = CreateAdjustmentSut(Principal(InventoryPermissions.Stock.Manage));
        SetupRecord(mediatorMock);
        var request = Movement(2.5m);
        request.QuantityDelta = -3;

        // Act
        var response = await controller.PostAsync(request);

        // Assert
        Assert.IsType<ObjectResult>(response);
        mediatorMock.Verify(m => m.Send(It.IsAny<RecordStockMovementCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PostAsync_ShouldDispatch_WhenAUnitCostIsGivenWithTheRevaluePermission()
    {
        // Arrange
        var (controller, mediatorMock) = CreateAdjustmentSut(
            Principal(InventoryPermissions.Stock.Manage, InventoryPermissions.Stock.Revalue));
        SetupRecord(mediatorMock);
        var request = Movement(2.5m);

        // Act
        var response = await controller.PostAsync(request);

        // Assert
        Assert.IsType<ObjectResult>(response);
        mediatorMock.Verify(
            m => m.Send(
                It.Is<RecordStockMovementCommand>(c => c.Model == request && c.PerformedByUserId == CurrentUserId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PostAsync_ShouldDispatch_WhenAUnitCostIsGivenByAFullControlUser()
    {
        // Arrange
        var user = InventoryCostAccessTests.PrincipalWith(SuperUserPolicy.SuperUserName);
        var (controller, mediatorMock) = CreateAdjustmentSut(user);
        SetupRecord(mediatorMock);

        // Act
        var response = await controller.PostAsync(Movement(2.5m));

        // Assert
        Assert.IsType<ObjectResult>(response);
    }

    [Fact]
    public async Task PostAsync_ShouldDispatch_WhenNoUnitCostIsGiven_WithoutTheRevaluePermission()
    {
        // Arrange — only Manage (checked by the endpoint attribute) is needed for a cost-free adjustment.
        var (controller, mediatorMock) = CreateAdjustmentSut(Principal(InventoryPermissions.Stock.Manage));
        SetupRecord(mediatorMock);

        // Act
        var response = await controller.PostAsync(Movement(null));

        // Assert
        Assert.IsType<ObjectResult>(response);
        mediatorMock.Verify(m => m.Send(It.IsAny<RecordStockMovementCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevalueAsync_ShouldDispatchTheCommand_AsTheCurrentUser()
    {
        // Arrange
        var (controller, mediatorMock) = CreateAdjustmentSut(Principal(InventoryPermissions.Stock.Revalue));
        var request = new RevalueStockRequest { ProductId = 1, LocationId = "location-1", UnitCost = 3m };
        var expected = Result.Success();
        mediatorMock
            .Setup(m => m.Send(
                It.Is<RevalueStockCommand>(c => c.Model == request && c.PerformedByUserId == CurrentUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.RevalueAsync(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SearchAsync_ShouldPassIncludeCost_FromTheViewCostPermission_ForAdjustments(bool hasViewCost)
    {
        // Arrange
        var user = hasViewCost ? Principal(InventoryPermissions.Stock.ViewCost) : Principal(InventoryPermissions.Stock.View);
        var (controller, mediatorMock) = CreateAdjustmentSut(user);
        mediatorMock
            .Setup(m => m.Send(It.IsAny<SearchStockAdjustmentsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<StockAdjustmentDto>([], 1, 20, 0));

        // Act
        await controller.SearchAsync(new SearchStockAdjustmentRequest());

        // Assert
        mediatorMock.Verify(
            m => m.Send(It.Is<SearchStockAdjustmentsQuery>(q => q.IncludeCost == hasViewCost), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SearchAsync_ShouldPassIncludeCost_FromTheViewCostPermission_ForLevels(bool hasViewCost)
    {
        // Arrange
        var user = hasViewCost ? Principal(InventoryPermissions.Stock.ViewCost) : Principal(InventoryPermissions.Stock.View);
        var (controller, mediatorMock) = CreateLevelSut(user);
        mediatorMock
            .Setup(m => m.Send(It.IsAny<SearchStockLevelsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<StockLevelDto>([], 1, 20, 0));

        // Act
        await controller.SearchAsync(new SearchStockLevelRequest());

        // Assert
        mediatorMock.Verify(
            m => m.Send(It.Is<SearchStockLevelsQuery>(q => q.IncludeCost == hasViewCost), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetValuationAsync_ShouldDispatchTheValuationQuery()
    {
        // Arrange
        var (controller, mediatorMock) = CreateLevelSut(Principal(InventoryPermissions.Stock.ViewCost));
        var request = new SearchStockValuationRequest { LocationId = "location-1" };
        var expected = Result<StockValuationDto>.Success(new StockValuationDto());
        mediatorMock
            .Setup(m => m.Send(It.Is<SearchStockValuationQuery>(q => q.Request == request), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.GetValuationAsync(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }
}
