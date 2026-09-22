using System.Reflection;
using System.Security.Claims;
using Light.AspNetCore.Authorization;
using Light.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Purchasing.Tests.TestSupport;
using StarterKit.Inventory.Contracts.Authorization;
using StarterKit.Purchasing.Api.Application.PurchaseOrders.Commands;
using StarterKit.Purchasing.Api.Application.PurchaseReturns.Queries;
using StarterKit.Purchasing.Api.Controllers;
using StarterKit.Purchasing.Contracts.Authorization;
using StarterKit.Shared.Authorization;
using StarterKit.Shared.Constants;

namespace Purchasing.Tests.Controllers;

/// <summary>
/// Capability helpers and mediator wiring are exercised directly; the <c>[MustHavePermission]</c> gates are
/// authorization middleware, so they are asserted by reflection on the attributes.
/// </summary>
public class PurchasingAccessAndControllerTests
{
    private const string CurrentUserId = "current-user";

    private static ClaimsPrincipal PrincipalWith(
        string? userName = null,
        params string[] permissions)
    {
        var claims = permissions.Select(x => new Claim(ClaimTypeConstants.Permission, x)).ToList();

        if (userName is not null)
            claims.Add(new Claim(ClaimTypeConstants.UserName, userName));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static ControllerContext ContextFor(
        Mock<IMediator> mediator,
        ClaimsPrincipal user) =>
        new()
        {
            HttpContext = new DefaultHttpContext
            {
                User = user,
                RequestServices = new ServiceCollection().AddSingleton(mediator.Object).BuildServiceProvider(),
            },
        };

    // PurchasingAccess

    [Fact]
    public void CanManageOrders_ShouldRequireOrdersClosePermissionOrFullControl()
    {
        Assert.False(PrincipalWith().CanManageOrders());
        Assert.False(PrincipalWith(null, PurchasingPermissions.Orders.Create, PurchasingPermissions.Orders.Submit).CanManageOrders());
        Assert.True(PrincipalWith(null, PurchasingPermissions.Orders.Close).CanManageOrders());
        Assert.True(PrincipalWith(SuperUserPolicy.SuperUserName).CanManageOrders());
    }

    [Fact]
    public void CanViewStockCost_ShouldRequireInventoryViewCostOrFullControl()
    {
        Assert.False(PrincipalWith().CanViewStockCost());
        Assert.False(PrincipalWith(null, PurchasingPermissions.Returns.View, PurchasingPermissions.Orders.Close).CanViewStockCost());
        Assert.True(PrincipalWith(null, InventoryPermissions.Stock.ViewCost).CanViewStockCost());
        Assert.True(PrincipalWith(SuperUserPolicy.SuperUserName).CanViewStockCost());
    }

    // Controllers

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task ReturnController_ShouldPassStockCostVisibilityToTheQueries(
        bool hasViewCost,
        bool expected)
    {
        var mediator = new Mock<IMediator>();
        var user = hasViewCost ? PrincipalWith(null, InventoryPermissions.Stock.ViewCost) : PrincipalWith();
        var controller = new PurchaseReturnController(new FakeCurrentUser { UserId = CurrentUserId })
        {
            ControllerContext = ContextFor(mediator, user),
        };
        mediator
            .Setup(m => m.Send(It.Is<GetPurchaseReturnByIdQuery>(q => q.Id == 3 && q.CanViewStockCost == expected), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PurchaseReturnDto>.Success(new PurchaseReturnDto()));
        mediator
            .Setup(m => m.Send(It.Is<SearchPurchaseReturnsQuery>(q => q.CanViewStockCost == expected), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<PurchaseReturnDto>([], 1, 20, 0));

        Assert.IsType<ObjectResult>(await controller.GetAsync(3));
        Assert.IsType<ObjectResult>(await controller.SearchAsync(new SearchPurchaseReturnRequest()));

        mediator.Verify(m => m.Send(It.IsAny<GetPurchaseReturnByIdQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        mediator.Verify(m => m.Send(It.IsAny<SearchPurchaseReturnsQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OrderController_ShouldPassTheManagerFlag_AndTheCurrentUser(bool manager)
    {
        var mediator = new Mock<IMediator>();
        var user = manager ? PrincipalWith(null, PurchasingPermissions.Orders.Close) : PrincipalWith();
        var controller = new PurchaseOrderController(new FakeCurrentUser { UserId = CurrentUserId })
        {
            ControllerContext = ContextFor(mediator, user),
        };
        mediator
            .Setup(m => m.Send(
                It.Is<CancelPurchaseOrderCommand>(c => c.Id == 4 && c.CurrentUserId == CurrentUserId && c.CanManage == manager),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        Assert.IsType<ObjectResult>(await controller.CancelAsync(4, new CancelPurchaseOrderRequest { Reason = "r" }));

        mediator.Verify(m => m.Send(It.IsAny<CancelPurchaseOrderCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Controllers_ShouldRequireAnAuthenticatedUserId()
    {
        Assert.Throws<ArgumentNullException>(() => new PurchaseOrderController(new FakeCurrentUser()));
        Assert.Throws<ArgumentNullException>(() => new PurchaseReturnController(new FakeCurrentUser()));
    }

    // Permission gating and registration

    private static string[] PoliciesOf(
        Type controller,
        string action) =>
        controller.GetMethod(action)!.GetCustomAttributes<MustHavePermissionAttribute>().Select(x => x.Policy!).ToArray();

    [Theory]
    [InlineData(nameof(PurchaseOrderController.PostAsync), PurchasingPermissions.Orders.Create)]
    [InlineData(nameof(PurchaseOrderController.CancelAsync), PurchasingPermissions.Orders.Create)]
    [InlineData(nameof(PurchaseOrderController.SubmitAsync), PurchasingPermissions.Orders.Submit)]
    [InlineData(nameof(PurchaseOrderController.WithdrawAsync), PurchasingPermissions.Orders.Submit)]
    [InlineData(nameof(PurchaseOrderController.ReceiveAsync), PurchasingPermissions.Receipts.Create)]
    [InlineData(nameof(PurchaseOrderController.CloseAsync), PurchasingPermissions.Orders.Close)]
    public void OrderActions_ShouldRequireTheirSpecificPermission(
        string action,
        string permission)
    {
        Assert.Contains(permission, PoliciesOf(typeof(PurchaseOrderController), action));
    }

    [Theory]
    [InlineData(nameof(PurchaseReturnController.PostAsync), PurchasingPermissions.Returns.Create)]
    [InlineData(nameof(PurchaseReturnController.PostReturnAsync), PurchasingPermissions.Returns.Create)]
    [InlineData(nameof(PurchaseReturnController.CancelAsync), PurchasingPermissions.Returns.Create)]
    [InlineData(nameof(PurchaseReturnController.MarkCreditedAsync), PurchasingPermissions.Returns.Credit)]
    public void ReturnActions_ShouldRequireTheirSpecificPermission(
        string action,
        string permission)
    {
        Assert.Contains(permission, PoliciesOf(typeof(PurchaseReturnController), action));
    }

    [Fact]
    public void PermissionProvider_ShouldRegisterEveryKeyOnce_IncludingReturnsCredit()
    {
        var keys = new PurchasingPermissionProvider().Define().Select(x => x.Name).ToList();

        Assert.Equal(11, keys.Count);
        Assert.Equal(keys.Count, keys.Distinct().Count());
        Assert.Contains(PurchasingPermissions.Returns.Credit, keys);
        Assert.Contains(PurchasingPermissions.Returns.Create, keys);
        Assert.Contains(PurchasingPermissions.Returns.View, keys);
        Assert.Contains(PurchasingPermissions.Receipts.Create, keys);
        Assert.Contains(PurchasingPermissions.Suppliers.Manage, keys);
        Assert.Equal("purchasing.returns.credit", PurchasingPermissions.Returns.Credit);
        Assert.All(keys, x => Assert.StartsWith("purchasing.", x));
    }
}
