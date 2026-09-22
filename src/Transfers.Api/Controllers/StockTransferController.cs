using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Infrastructure.Endpoints;
using StarterKit.Shared;
using StarterKit.Transfers.Api.Application.StockTransfers.Commands;
using StarterKit.Transfers.Api.Application.StockTransfers.Queries;
using StarterKit.Transfers.Contracts.Authorization;

namespace StarterKit.Transfers.Api.Controllers;

[ApiExplorerSettings(GroupName = "transfers")]
[Route("api/v{version:apiVersion}/stock_transfer")]
[MustHavePermission(TransfersPermissions.Transfers.View)]
public class StockTransferController(ICurrentUser currentUser) : VersionedApiController
{
    private readonly string _currentUserId = currentUser.UserId
        ?? throw new ArgumentNullException(nameof(currentUser.UserId));

    [HttpGet]
    public async Task<IActionResult> SearchAsync([FromQuery] SearchStockTransferRequest request)
    {
        return Ok(await Mediator.Send(new SearchStockTransfersQuery(request, User.CanViewCost())));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync([FromRoute] long id)
    {
        return Ok(await Mediator.Send(new GetStockTransferByIdQuery(id, User.CanViewCost())));
    }

    [HttpPost]
    [MustHavePermission(TransfersPermissions.Transfers.Create)]
    public async Task<IActionResult> PostAsync([FromBody] CreateStockTransferRequest request)
    {
        return Ok(await Mediator.Send(new CreateStockTransferCommand(request)));
    }

    [HttpPut("{id}")]
    [MustHavePermission(TransfersPermissions.Transfers.Create)]
    public async Task<IActionResult> PutAsync([FromRoute] long id, [FromBody] UpdateStockTransferRequest request)
    {
        return Ok(await Mediator.Send(new UpdateStockTransferCommand(id, request)));
    }

    [HttpPost("{id}/line")]
    [MustHavePermission(TransfersPermissions.Transfers.Create)]
    public async Task<IActionResult> AddLineAsync([FromRoute] long id, [FromBody] AddStockTransferLineRequest request)
    {
        return Ok(await Mediator.Send(new AddStockTransferLineCommand(id, request)));
    }

    [HttpPut("{id}/line/{lineId}")]
    [MustHavePermission(TransfersPermissions.Transfers.Create)]
    public async Task<IActionResult> UpdateLineAsync(
        [FromRoute] long id,
        [FromRoute] long lineId,
        [FromBody] UpdateStockTransferLineRequest request)
    {
        return Ok(await Mediator.Send(new UpdateStockTransferLineCommand(id, lineId, request)));
    }

    [HttpDelete("{id}/line/{lineId}")]
    [MustHavePermission(TransfersPermissions.Transfers.Create)]
    public async Task<IActionResult> RemoveLineAsync([FromRoute] long id, [FromRoute] long lineId)
    {
        return Ok(await Mediator.Send(new RemoveStockTransferLineCommand(id, lineId)));
    }

    [HttpPut("{id}/dispatch")]
    [MustHavePermission(TransfersPermissions.Transfers.Dispatch)]
    public async Task<IActionResult> DispatchAsync([FromRoute] long id)
    {
        return Ok(await Mediator.Send(new DispatchStockTransferCommand(id, _currentUserId)));
    }

    [HttpPost("{id}/receipt")]
    [MustHavePermission(TransfersPermissions.Transfers.Receive)]
    public async Task<IActionResult> ReceiveAsync([FromRoute] long id, [FromBody] ReceiveStockTransferRequest request)
    {
        return Ok(await Mediator.Send(new ReceiveStockTransferCommand(id, request, _currentUserId)));
    }

    [HttpPut("{id}/close")]
    [MustHavePermission(TransfersPermissions.Transfers.Close)]
    public async Task<IActionResult> CloseAsync([FromRoute] long id, [FromBody] CloseStockTransferRequest request)
    {
        return Ok(await Mediator.Send(new CloseStockTransferCommand(id, request)));
    }

    [HttpPut("{id}/cancel")]
    [MustHavePermission(TransfersPermissions.Transfers.Create)]
    public async Task<IActionResult> CancelAsync([FromRoute] long id, [FromBody] CancelStockTransferRequest request)
    {
        return Ok(await Mediator.Send(new CancelStockTransferCommand(id, request)));
    }
}
