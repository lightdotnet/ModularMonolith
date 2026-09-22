using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Infrastructure.Endpoints;
using StarterKit.Inventory.Api.Application.StockAdjustments.Commands;
using StarterKit.Inventory.Api.Application.StockAdjustments.Queries;
using StarterKit.Inventory.Contracts.Authorization;
using StarterKit.Shared;

namespace StarterKit.Inventory.Api.Controllers;

[ApiExplorerSettings(GroupName = "inventory")]
[Route("api/v{version:apiVersion}/stock_adjustment")]
[MustHavePermission(InventoryPermissions.Stock.View)]
public class StockAdjustmentController(ICurrentUser currentUser) : VersionedApiController
{
    private readonly string _currentUserId = currentUser.UserId
        ?? throw new ArgumentNullException(nameof(currentUser.UserId));

    [HttpGet]
    public async Task<IActionResult> SearchAsync([FromQuery] SearchStockAdjustmentRequest request)
    {
        return Ok(await Mediator.Send(new SearchStockAdjustmentsQuery(
            request,
            User.CanViewCost())));
    }

    [HttpPost]
    [MustHavePermission(InventoryPermissions.Stock.Manage)]
    public async Task<IActionResult> PostAsync([FromBody] RecordStockMovementRequest request)
    {
        // Manage is enough to supply the unit cost of an inbound adjustment (needed to first-stock a product);
        // the revalue permission is only required to change the value of stock already on hand (see below).
        return Ok(await Mediator.Send(new RecordStockMovementCommand(request, _currentUserId)));
    }

    [HttpPost("revaluation")]
    [MustHavePermission(InventoryPermissions.Stock.Revalue)]
    public async Task<IActionResult> RevalueAsync([FromBody] RevalueStockRequest request)
    {
        return Ok(await Mediator.Send(new RevalueStockCommand(request, _currentUserId)));
    }
}
