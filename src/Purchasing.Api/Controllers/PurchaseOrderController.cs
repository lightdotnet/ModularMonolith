using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Infrastructure.Endpoints;
using StarterKit.Purchasing.Api.Application.PurchaseOrders.Commands;
using StarterKit.Purchasing.Api.Application.PurchaseOrders.Queries;
using StarterKit.Purchasing.Contracts.Authorization;
using StarterKit.Shared;
using StarterKit.Shared.Extensions;

namespace StarterKit.Purchasing.Api.Controllers;

/// <summary>
/// There is deliberately no approve/decide endpoint: an order is decided by the approver chosen at
/// submission, on the decision surface of the Approval module, and the outcome flows back through the
/// <c>ApprovalFinalizedIntegrationEvent</c>.
/// </summary>
[ApiExplorerSettings(GroupName = "purchasing")]
[Route("api/v{version:apiVersion}/purchase_order")]
[MustHavePermission(PurchasingPermissions.Orders.View)]
public class PurchaseOrderController(ICurrentUser currentUser) : VersionedApiController
{
    private readonly string _currentUserId = currentUser.UserId
        ?? throw new ArgumentNullException(nameof(currentUser.UserId));

    [HttpGet]
    public async Task<IActionResult> SearchAsync([FromQuery] SearchPurchaseOrderRequest request)
    {
        return Ok(await Mediator.Send(new SearchPurchaseOrdersQuery(request)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync([FromRoute] long id)
    {
        return Ok(await Mediator.Send(new GetPurchaseOrderByIdQuery(id)));
    }

    [HttpGet("approvers")]
    [MustHavePermission(PurchasingPermissions.Orders.Submit)]
    public async Task<IActionResult> GetApproversAsync()
    {
        return Ok(await Mediator.Send(new GetPurchaseOrderApproverCandidatesQuery(User.GetEmployeeId())));
    }

    [HttpPost]
    [MustHavePermission(PurchasingPermissions.Orders.Create)]
    public async Task<IActionResult> PostAsync([FromBody] CreatePurchaseOrderRequest request)
    {
        return Ok(await Mediator.Send(new CreatePurchaseOrderCommand(
            request,
            _currentUserId,
            User.GetEmployeeId())));
    }

    [HttpPut("{id}")]
    [MustHavePermission(PurchasingPermissions.Orders.Create)]
    public async Task<IActionResult> PutAsync([FromRoute] long id, [FromBody] UpdatePurchaseOrderRequest request)
    {
        return Ok(await Mediator.Send(new UpdatePurchaseOrderCommand(id, request, _currentUserId, User.CanManageOrders())));
    }

    [HttpPost("{id}/line")]
    [MustHavePermission(PurchasingPermissions.Orders.Create)]
    public async Task<IActionResult> AddLineAsync([FromRoute] long id, [FromBody] AddPurchaseOrderLineRequest request)
    {
        return Ok(await Mediator.Send(new AddPurchaseOrderLineCommand(id, request, _currentUserId, User.CanManageOrders())));
    }

    [HttpPut("{id}/line/{lineId}")]
    [MustHavePermission(PurchasingPermissions.Orders.Create)]
    public async Task<IActionResult> UpdateLineAsync(
        [FromRoute] long id,
        [FromRoute] long lineId,
        [FromBody] UpdatePurchaseOrderLineRequest request)
    {
        return Ok(await Mediator.Send(new UpdatePurchaseOrderLineCommand(id, lineId, request, _currentUserId, User.CanManageOrders())));
    }

    [HttpDelete("{id}/line/{lineId}")]
    [MustHavePermission(PurchasingPermissions.Orders.Create)]
    public async Task<IActionResult> RemoveLineAsync([FromRoute] long id, [FromRoute] long lineId)
    {
        return Ok(await Mediator.Send(new RemovePurchaseOrderLineCommand(id, lineId, _currentUserId, User.CanManageOrders())));
    }

    [HttpPut("{id}/submit")]
    [MustHavePermission(PurchasingPermissions.Orders.Submit)]
    public async Task<IActionResult> SubmitAsync([FromRoute] long id, [FromBody] SubmitPurchaseOrderRequest request)
    {
        return Ok(await Mediator.Send(new SubmitPurchaseOrderCommand(id, request, _currentUserId)));
    }

    [HttpPut("{id}/withdraw")]
    [MustHavePermission(PurchasingPermissions.Orders.Submit)]
    public async Task<IActionResult> WithdrawAsync([FromRoute] long id)
    {
        return Ok(await Mediator.Send(new WithdrawPurchaseOrderCommand(id, _currentUserId)));
    }

    [HttpPost("{id}/receipt")]
    [MustHavePermission(PurchasingPermissions.Receipts.Create)]
    public async Task<IActionResult> ReceiveAsync([FromRoute] long id, [FromBody] ReceivePurchaseOrderRequest request)
    {
        return Ok(await Mediator.Send(new ReceivePurchaseOrderCommand(id, request, _currentUserId)));
    }

    [HttpPut("{id}/close")]
    [MustHavePermission(PurchasingPermissions.Orders.Close)]
    public async Task<IActionResult> CloseAsync([FromRoute] long id, [FromBody] ClosePurchaseOrderRequest request)
    {
        return Ok(await Mediator.Send(new ClosePurchaseOrderCommand(id, request, _currentUserId)));
    }

    [HttpPut("{id}/cancel")]
    [MustHavePermission(PurchasingPermissions.Orders.Create)]
    public async Task<IActionResult> CancelAsync([FromRoute] long id, [FromBody] CancelPurchaseOrderRequest request)
    {
        return Ok(await Mediator.Send(new CancelPurchaseOrderCommand(id, request, _currentUserId, User.CanManageOrders())));
    }
}
