using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Orders.Api.Application.Orders.Commands;
using StarterKit.Orders.Api.Application.Orders.Queries;
using StarterKit.Orders.Contracts.Authorization;
using StarterKit.Infrastructure.Endpoints;
using StarterKit.Shared;

namespace StarterKit.Orders.Api.Controllers;

[ApiExplorerSettings(GroupName = "orders")]
[Route("api/v{version:apiVersion}/order")]
[MustHavePermission(OrdersPermissions.Orders.View)]
public class OrderController(ICurrentUser currentUser) : VersionedApiController
{
    private readonly string _currentUserId = currentUser.UserId
        ?? throw new ArgumentNullException(nameof(currentUser.UserId));

    [HttpGet]
    public async Task<IActionResult> SearchAsync([FromQuery] SearchOrderRequest request)
    {
        return Ok(await Mediator.Send(new SearchOrdersQuery(request)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync([FromRoute] long id)
    {
        return Ok(await Mediator.Send(new GetOrderByIdQuery(id)));
    }

    [HttpPost]
    [MustHavePermission(OrdersPermissions.Orders.Manage)]
    public async Task<IActionResult> PostAsync([FromBody] CreateOrderRequest request)
    {
        return Ok(await Mediator.Send(new CreateOrderCommand(request)));
    }

    [HttpPost("{id}/line")]
    [MustHavePermission(OrdersPermissions.Orders.Manage)]
    public async Task<IActionResult> AddLineAsync([FromRoute] long id, [FromBody] AddOrderLineRequest request)
    {
        return Ok(await Mediator.Send(new AddOrderLineCommand(id, request)));
    }

    [HttpPut("{id}/line/{lineId}/quantity")]
    [MustHavePermission(OrdersPermissions.Orders.Manage)]
    public async Task<IActionResult> UpdateLineQuantityAsync(
        [FromRoute] long id,
        [FromRoute] long lineId,
        [FromBody] UpdateOrderLineQuantityRequest request)
    {
        return Ok(await Mediator.Send(new UpdateOrderLineQuantityCommand(id, lineId, request)));
    }

    [HttpPut("{id}/line/{lineId}/sale_price")]
    [MustHavePermission(OrdersPermissions.Orders.Manage)]
    public async Task<IActionResult> SetLineSalePriceAsync(
        [FromRoute] long id,
        [FromRoute] long lineId,
        [FromBody] SetOrderLineSalePriceRequest request)
    {
        return Ok(await Mediator.Send(new SetOrderLineSalePriceCommand(id, lineId, request)));
    }

    [HttpDelete("{id}/line/{lineId}")]
    [MustHavePermission(OrdersPermissions.Orders.Manage)]
    public async Task<IActionResult> RemoveLineAsync([FromRoute] long id, [FromRoute] long lineId)
    {
        return Ok(await Mediator.Send(new RemoveOrderLineCommand(id, lineId)));
    }

    [HttpPut("{id}/discount")]
    [MustHavePermission(OrdersPermissions.Orders.Manage)]
    public async Task<IActionResult> ApplyDiscountAsync([FromRoute] long id, [FromBody] ApplyOrderDiscountRequest request)
    {
        return Ok(await Mediator.Send(new ApplyOrderDiscountCommand(id, request)));
    }

    [HttpDelete("{id}/discount")]
    [MustHavePermission(OrdersPermissions.Orders.Manage)]
    public async Task<IActionResult> RemoveDiscountAsync([FromRoute] long id)
    {
        return Ok(await Mediator.Send(new RemoveOrderDiscountCommand(id)));
    }

    [HttpPost("{id}/fee")]
    [MustHavePermission(OrdersPermissions.Orders.Manage)]
    public async Task<IActionResult> AddFeeAsync([FromRoute] long id, [FromBody] AddOrderFeeRequest request)
    {
        return Ok(await Mediator.Send(new AddOrderFeeCommand(id, request)));
    }

    [HttpDelete("{id}/fee/{feeId}")]
    [MustHavePermission(OrdersPermissions.Orders.Manage)]
    public async Task<IActionResult> RemoveFeeAsync([FromRoute] long id, [FromRoute] long feeId)
    {
        return Ok(await Mediator.Send(new RemoveOrderFeeCommand(id, feeId)));
    }

    [HttpPut("{id}/place")]
    [MustHavePermission(OrdersPermissions.Orders.Manage)]
    public async Task<IActionResult> PlaceAsync([FromRoute] long id)
    {
        return Ok(await Mediator.Send(new PlaceOrderCommand(id, _currentUserId)));
    }

    [HttpPut("{id}/cancel")]
    [MustHavePermission(OrdersPermissions.Orders.Manage)]
    public async Task<IActionResult> CancelAsync([FromRoute] long id, [FromBody] CancelOrderRequest request)
    {
        return Ok(await Mediator.Send(new CancelOrderCommand(id, request, _currentUserId)));
    }

    [HttpPut("{id}/fulfill")]
    [MustHavePermission(OrdersPermissions.Orders.Manage)]
    public async Task<IActionResult> MarkFulfilledAsync([FromRoute] long id)
    {
        return Ok(await Mediator.Send(new MarkOrderFulfilledCommand(id)));
    }
}
