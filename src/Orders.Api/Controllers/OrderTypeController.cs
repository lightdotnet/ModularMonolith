using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Infrastructure.Endpoints;
using StarterKit.Orders.Api.Application.OrderTypes.Commands;
using StarterKit.Orders.Api.Application.OrderTypes.Queries;
using StarterKit.Orders.Contracts.Authorization;

namespace StarterKit.Orders.Api.Controllers;

[ApiExplorerSettings(GroupName = "orders")]
[Route("api/v{version:apiVersion}/order_type")]
[MustHavePermission(OrdersPermissions.OrderTypes.View)]
public class OrderTypeController : VersionedApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAsync([FromQuery] OrderTypeCategory? category)
    {
        return Ok(await Mediator.Send(new GetOrderTypesQuery(category)));
    }

    [HttpGet("{category}/{id}")]
    public async Task<IActionResult> GetByIdAsync([FromRoute] OrderTypeCategory category, [FromRoute] string id)
    {
        return Ok(await Mediator.Send(new GetOrderTypeByIdQuery(id, category)));
    }

    [HttpPost]
    [MustHavePermission(OrdersPermissions.OrderTypes.Manage)]
    public async Task<IActionResult> PostAsync([FromBody] CreateOrderTypeRequest request)
    {
        return Ok(await Mediator.Send(new CreateOrderTypeCommand(request)));
    }

    [HttpPut("{category}/{id}")]
    [MustHavePermission(OrdersPermissions.OrderTypes.Manage)]
    public async Task<IActionResult> PutAsync(
        [FromRoute] OrderTypeCategory category,
        [FromRoute] string id,
        [FromBody] UpdateOrderTypeRequest request)
    {
        return Ok(await Mediator.Send(new UpdateOrderTypeCommand(id, category, request)));
    }

    [HttpDelete("{category}/{id}")]
    [MustHavePermission(OrdersPermissions.OrderTypes.Manage)]
    public async Task<IActionResult> DeleteAsync([FromRoute] OrderTypeCategory category, [FromRoute] string id)
    {
        return Ok(await Mediator.Send(new DeleteOrderTypeCommand(id, category)));
    }
}
