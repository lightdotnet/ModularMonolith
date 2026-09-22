using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Infrastructure.Endpoints;
using StarterKit.Purchasing.Api.Application.GoodsReceipts.Queries;
using StarterKit.Purchasing.Contracts.Authorization;

namespace StarterKit.Purchasing.Api.Controllers;

/// <summary>
/// Read-only: a goods receipt is created by receiving against a purchase order
/// (<c>POST purchase_order/{id}/receipt</c>) and is immutable afterwards.
/// </summary>
[ApiExplorerSettings(GroupName = "purchasing")]
[Route("api/v{version:apiVersion}/goods_receipt")]
[MustHavePermission(PurchasingPermissions.Receipts.View)]
public class GoodsReceiptController : VersionedApiController
{
    [HttpGet]
    public async Task<IActionResult> SearchAsync([FromQuery] SearchGoodsReceiptRequest request)
    {
        return Ok(await Mediator.Send(new SearchGoodsReceiptsQuery(request)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync([FromRoute] long id)
    {
        return Ok(await Mediator.Send(new GetGoodsReceiptByIdQuery(id)));
    }
}
