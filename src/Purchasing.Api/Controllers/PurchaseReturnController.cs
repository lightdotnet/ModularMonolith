using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Infrastructure.Endpoints;
using StarterKit.Purchasing.Api.Application.PurchaseReturns.Commands;
using StarterKit.Purchasing.Api.Application.PurchaseReturns.Queries;
using StarterKit.Purchasing.Contracts.Authorization;
using StarterKit.Shared;

namespace StarterKit.Purchasing.Api.Controllers;

[ApiExplorerSettings(GroupName = "purchasing")]
[Route("api/v{version:apiVersion}/purchase_return")]
[MustHavePermission(PurchasingPermissions.Returns.View)]
public class PurchaseReturnController(ICurrentUser currentUser) : VersionedApiController
{
    private readonly string _currentUserId = currentUser.UserId
        ?? throw new ArgumentNullException(nameof(currentUser.UserId));

    [HttpGet]
    public async Task<IActionResult> SearchAsync([FromQuery] SearchPurchaseReturnRequest request)
    {
        return Ok(await Mediator.Send(new SearchPurchaseReturnsQuery(request, User.CanViewStockCost())));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync([FromRoute] long id)
    {
        return Ok(await Mediator.Send(new GetPurchaseReturnByIdQuery(id, User.CanViewStockCost())));
    }

    [HttpPost]
    [MustHavePermission(PurchasingPermissions.Returns.Create)]
    public async Task<IActionResult> PostAsync([FromBody] CreatePurchaseReturnRequest request)
    {
        return Ok(await Mediator.Send(new CreatePurchaseReturnCommand(request)));
    }

    [HttpPut("{id}")]
    [MustHavePermission(PurchasingPermissions.Returns.Create)]
    public async Task<IActionResult> PutAsync([FromRoute] long id, [FromBody] UpdatePurchaseReturnRequest request)
    {
        return Ok(await Mediator.Send(new UpdatePurchaseReturnCommand(id, request)));
    }

    [HttpPut("{id}/post")]
    [MustHavePermission(PurchasingPermissions.Returns.Create)]
    public async Task<IActionResult> PostReturnAsync([FromRoute] long id)
    {
        return Ok(await Mediator.Send(new PostPurchaseReturnCommand(id, _currentUserId)));
    }

    [HttpPut("{id}/cancel")]
    [MustHavePermission(PurchasingPermissions.Returns.Create)]
    public async Task<IActionResult> CancelAsync([FromRoute] long id, [FromBody] CancelPurchaseReturnRequest request)
    {
        return Ok(await Mediator.Send(new CancelPurchaseReturnCommand(id, request, _currentUserId)));
    }

    [HttpPut("{id}/credit")]
    [MustHavePermission(PurchasingPermissions.Returns.Credit)]
    public async Task<IActionResult> MarkCreditedAsync([FromRoute] long id, [FromBody] MarkPurchaseReturnCreditedRequest request)
    {
        return Ok(await Mediator.Send(new MarkPurchaseReturnCreditedCommand(id, request, _currentUserId)));
    }
}
