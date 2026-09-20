using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Infrastructure.Endpoints;
using StarterKit.Inventory.Api.Application.StockLevels.Queries;
using StarterKit.Inventory.Contracts.Authorization;

namespace StarterKit.Inventory.Api.Controllers;

[ApiExplorerSettings(GroupName = "inventory")]
[Route("api/v{version:apiVersion}/stock_level")]
[MustHavePermission(InventoryPermissions.Stock.View)]
public class StockLevelController : VersionedApiController
{
    [HttpGet]
    public async Task<IActionResult> SearchAsync([FromQuery] SearchStockLevelRequest request)
    {
        return Ok(await Mediator.Send(new SearchStockLevelsQuery(request)));
    }

    [HttpGet("total/{productId}")]
    public async Task<IActionResult> GetTotalAsync([FromRoute] long productId)
    {
        return Ok(await Mediator.Send(new GetProductStockTotalQuery(productId)));
    }
}
