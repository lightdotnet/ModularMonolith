using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Infrastructure.Endpoints;
using StarterKit.Purchasing.Api.Application.Suppliers.Commands;
using StarterKit.Purchasing.Api.Application.Suppliers.Queries;
using StarterKit.Purchasing.Contracts.Authorization;

namespace StarterKit.Purchasing.Api.Controllers;

[ApiExplorerSettings(GroupName = "purchasing")]
[Route("api/v{version:apiVersion}/supplier")]
[MustHavePermission(PurchasingPermissions.Suppliers.View)]
public class SupplierController : VersionedApiController
{
    [HttpGet]
    public async Task<IActionResult> SearchAsync([FromQuery] SearchSupplierRequest request)
    {
        return Ok(await Mediator.Send(new SearchSuppliersQuery(request)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync([FromRoute] long id)
    {
        return Ok(await Mediator.Send(new GetSupplierByIdQuery(id)));
    }

    [HttpPost]
    [MustHavePermission(PurchasingPermissions.Suppliers.Manage)]
    public async Task<IActionResult> PostAsync([FromBody] CreateSupplierRequest request)
    {
        return Ok(await Mediator.Send(new CreateSupplierCommand(request)));
    }

    [HttpPut("{id}")]
    [MustHavePermission(PurchasingPermissions.Suppliers.Manage)]
    public async Task<IActionResult> PutAsync([FromRoute] long id, [FromBody] UpdateSupplierRequest request)
    {
        return Ok(await Mediator.Send(new UpdateSupplierCommand(id, request)));
    }

    [HttpPut("{id}/activate")]
    [MustHavePermission(PurchasingPermissions.Suppliers.Manage)]
    public async Task<IActionResult> ActivateAsync([FromRoute] long id)
    {
        return Ok(await Mediator.Send(new ChangeSupplierStatusCommand(id, Activate: true)));
    }

    [HttpPut("{id}/deactivate")]
    [MustHavePermission(PurchasingPermissions.Suppliers.Manage)]
    public async Task<IActionResult> DeactivateAsync([FromRoute] long id)
    {
        return Ok(await Mediator.Send(new ChangeSupplierStatusCommand(id, Activate: false)));
    }
}
