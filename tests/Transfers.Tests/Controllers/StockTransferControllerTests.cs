using System.Reflection;
using System.Security.Claims;
using Light.AspNetCore.Authorization;
using Light.Contracts;
using Light.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StarterKit.Inventory.Contracts.Authorization;
using StarterKit.Shared.Authorization;
using StarterKit.Shared.Constants;
using StarterKit.Transfers.Api.Application.StockTransfers.Commands;
using StarterKit.Transfers.Api.Application.StockTransfers.Queries;
using StarterKit.Transfers.Api.Controllers;
using StarterKit.Transfers.Contracts.Authorization;
using StarterKit.Transfers.Contracts.StockTransfers;
using Transfers.Tests.TestSupport;

namespace Transfers.Tests.Controllers;

/// <summary>
/// Mediator-dispatch wiring and the cost-visibility rule are exercised directly; the
/// <c>[MustHavePermission]</c> gates are authorization middleware, so they are asserted by reflection
/// on the attributes rather than by running the pipeline.
/// </summary>
public class StockTransferControllerTests
{
    private const string CurrentUserId = "current-user";

    private static ClaimsPrincipal PrincipalWith(
        string? userName = null,
        params string[] permissions)
    {
        var claims = permissions
            .Select(x => new Claim(ClaimTypeConstants.Permission, x))
            .ToList();

        if (userName is not null)
            claims.Add(new Claim(ClaimTypeConstants.UserName, userName));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static (StockTransferController Controller, Mock<IMediator> Mediator) CreateSut(ClaimsPrincipal? user = null)
    {
        var mediatorMock = new Mock<IMediator>();
        var httpContext = new DefaultHttpContext
        {
            User = user ?? PrincipalWith(),
            RequestServices = new ServiceCollection().AddSingleton(mediatorMock.Object).BuildServiceProvider(),
        };
        var controller = new StockTransferController(new FakeCurrentUser { UserId = CurrentUserId })
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };

        return (controller, mediatorMock);
    }

    [Fact]
    public void Ctor_ShouldRequireAnAuthenticatedUserId()
    {
        Assert.Throws<ArgumentNullException>(() => new StockTransferController(new FakeCurrentUser()));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task GetAsync_ShouldPassTheCostVisibilityOfTheCaller(
        bool hasViewCost,
        bool expected)
    {
        var (controller, mediator) = CreateSut(hasViewCost ? PrincipalWith(null, InventoryPermissions.Stock.ViewCost) : PrincipalWith());
        var payload = Result<StockTransferDto>.Success(new StockTransferDto());
        mediator
            .Setup(m => m.Send(It.Is<GetStockTransferByIdQuery>(q => q.Id == 7 && q.CanViewCost == expected), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload);

        var response = await controller.GetAsync(7);

        Assert.Same(payload, Assert.IsType<ObjectResult>(response).Value);
    }

    [Fact]
    public async Task SearchAsync_ShouldTreatFullControlAsViewCost()
    {
        var (controller, mediator) = CreateSut(PrincipalWith(SuperUserPolicy.SuperUserName));
        var request = new SearchStockTransferRequest();
        var expected = new PagedResult<StockTransferDto>([], 1, 20, 0);
        mediator
            .Setup(m => m.Send(It.Is<SearchStockTransfersQuery>(q => q.Request == request && q.CanViewCost), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var response = await controller.SearchAsync(request);

        Assert.Same(expected, Assert.IsType<ObjectResult>(response).Value);
    }

    [Fact]
    public async Task DispatchAsync_And_ReceiveAsync_ShouldStampTheCurrentUser()
    {
        var (controller, mediator) = CreateSut();
        mediator
            .Setup(m => m.Send(It.Is<DispatchStockTransferCommand>(c => c.Id == 3 && c.DispatchedByUserId == CurrentUserId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        mediator
            .Setup(m => m.Send(It.Is<ReceiveStockTransferCommand>(c => c.Id == 3 && c.ReceivedByUserId == CurrentUserId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<long>.Success(9));

        Assert.IsType<ObjectResult>(await controller.DispatchAsync(3));
        Assert.IsType<ObjectResult>(await controller.ReceiveAsync(3, new ReceiveStockTransferRequest()));

        mediator.Verify(m => m.Send(It.IsAny<DispatchStockTransferCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        mediator.Verify(m => m.Send(It.IsAny<ReceiveStockTransferCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EditingAndClosingActions_ShouldDispatchTheirCommands()
    {
        var (controller, mediator) = CreateSut();
        mediator.Setup(m => m.Send(It.IsAny<CloseStockTransferCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        mediator.Setup(m => m.Send(It.IsAny<CancelStockTransferCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        mediator.Setup(m => m.Send(It.IsAny<AddStockTransferLineCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        mediator.Setup(m => m.Send(It.IsAny<UpdateStockTransferLineCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        mediator.Setup(m => m.Send(It.IsAny<RemoveStockTransferLineCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        mediator.Setup(m => m.Send(It.IsAny<UpdateStockTransferCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        mediator.Setup(m => m.Send(It.IsAny<CreateStockTransferCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result<long>.Success(1));

        await controller.CloseAsync(1, new CloseStockTransferRequest { Reason = "r" });
        await controller.CancelAsync(1, new CancelStockTransferRequest { Reason = "r" });
        await controller.AddLineAsync(1, new AddStockTransferLineRequest());
        await controller.UpdateLineAsync(1, 2, new UpdateStockTransferLineRequest());
        await controller.RemoveLineAsync(1, 2);
        await controller.PutAsync(1, new UpdateStockTransferRequest());
        await controller.PostAsync(new CreateStockTransferRequest());

        mediator.Verify(m => m.Send(It.Is<CloseStockTransferCommand>(c => c.Id == 1), It.IsAny<CancellationToken>()), Times.Once);
        mediator.Verify(m => m.Send(It.Is<UpdateStockTransferLineCommand>(c => c.TransferId == 1 && c.TransferLineId == 2), It.IsAny<CancellationToken>()), Times.Once);
        mediator.Verify(m => m.Send(It.Is<RemoveStockTransferLineCommand>(c => c.TransferId == 1 && c.TransferLineId == 2), It.IsAny<CancellationToken>()), Times.Once);
    }

    // Permission gating (attributes)

    private static string[] PermissionsOf(string actionName) =>
        typeof(StockTransferController)
            .GetMethod(actionName)!
            .GetCustomAttributes<MustHavePermissionAttribute>()
            .Select(x => x.Policy!)
            .ToArray();

    [Fact]
    public void TheController_ShouldRequireTheViewPermission_ByDefault()
    {
        var permissions = typeof(StockTransferController)
            .GetCustomAttributes<MustHavePermissionAttribute>()
            .Select(x => x.Policy!);

        Assert.Contains(TransfersPermissions.Transfers.View, permissions);
    }

    [Theory]
    [InlineData(nameof(StockTransferController.PostAsync), TransfersPermissions.Transfers.Create)]
    [InlineData(nameof(StockTransferController.PutAsync), TransfersPermissions.Transfers.Create)]
    [InlineData(nameof(StockTransferController.AddLineAsync), TransfersPermissions.Transfers.Create)]
    [InlineData(nameof(StockTransferController.UpdateLineAsync), TransfersPermissions.Transfers.Create)]
    [InlineData(nameof(StockTransferController.RemoveLineAsync), TransfersPermissions.Transfers.Create)]
    [InlineData(nameof(StockTransferController.CancelAsync), TransfersPermissions.Transfers.Create)]
    [InlineData(nameof(StockTransferController.DispatchAsync), TransfersPermissions.Transfers.Dispatch)]
    [InlineData(nameof(StockTransferController.ReceiveAsync), TransfersPermissions.Transfers.Receive)]
    [InlineData(nameof(StockTransferController.CloseAsync), TransfersPermissions.Transfers.Close)]
    public void WriteActions_ShouldRequireTheirSpecificPermission(
        string action,
        string permission)
    {
        Assert.Contains(permission, PermissionsOf(action));
    }

    [Fact]
    public void PermissionProvider_ShouldRegisterEveryTransfersPermissionUnderTheGroup()
    {
        var definitions = new TransfersPermissionProvider().Define().ToList();
        var keys = definitions.Select(x => x.Name).ToList();

        Assert.Equal(5, keys.Count);
        Assert.Equal(keys.Count, keys.Distinct().Count());
        Assert.Contains(TransfersPermissions.Transfers.View, keys);
        Assert.Contains(TransfersPermissions.Transfers.Create, keys);
        Assert.Contains(TransfersPermissions.Transfers.Dispatch, keys);
        Assert.Contains(TransfersPermissions.Transfers.Receive, keys);
        Assert.Contains(TransfersPermissions.Transfers.Close, keys);
        Assert.All(keys, x => Assert.StartsWith("transfers.", x));
    }
}
