using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Orders.Api.Application.Payments.Commands;
using StarterKit.Orders.Api.Application.Payments.Queries;
using StarterKit.Orders.Contracts.Authorization;
using StarterKit.Infrastructure.Endpoints;
using StarterKit.Shared;

namespace StarterKit.Orders.Api.Controllers;

[ApiExplorerSettings(GroupName = "orders")]
[Route("api/v{version:apiVersion}/payment")]
[MustHavePermission(OrdersPermissions.Payments.View)]
public class PaymentController(ICurrentUser currentUser) : VersionedApiController
{
    private readonly string _currentUserId = currentUser.UserId
        ?? throw new ArgumentNullException(nameof(currentUser.UserId));

    [HttpGet("order/{orderId}")]
    public async Task<IActionResult> GetByOrderAsync([FromRoute] long orderId)
    {
        return Ok(await Mediator.Send(new GetPaymentsByOrderQuery(orderId)));
    }

    [HttpPost("order/{orderId}")]
    [MustHavePermission(OrdersPermissions.Payments.Manage)]
    public async Task<IActionResult> PostAsync([FromRoute] long orderId, [FromBody] RecordPaymentRequest request)
    {
        return Ok(await Mediator.Send(new RecordPaymentCommand(orderId, request, _currentUserId)));
    }

    [HttpPut("{id}/void")]
    [MustHavePermission(OrdersPermissions.Payments.Manage)]
    public async Task<IActionResult> VoidAsync([FromRoute] long id, [FromBody] VoidPaymentRequest request)
    {
        return Ok(await Mediator.Send(new VoidPaymentCommand(id, request)));
    }
}
