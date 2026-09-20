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
        return Ok(await Mediator.Send(new SearchStockAdjustmentsQuery(request)));
    }

    [HttpPost]
    [MustHavePermission(InventoryPermissions.Stock.Manage)]
    public async Task<IActionResult> PostAsync([FromBody] RecordStockMovementRequest request)
    {
        return Ok(await Mediator.Send(new RecordStockMovementCommand(request, _currentUserId)));
    }
}
