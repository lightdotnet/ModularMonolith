using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Catalog.Api.Application.Products.Commands;
using StarterKit.Catalog.Api.Application.Products.Queries;
using StarterKit.Catalog.Contracts.Authorization;
using StarterKit.Infrastructure.Endpoints;

namespace StarterKit.Catalog.Api.Controllers;

[ApiExplorerSettings(GroupName = "catalog")]
[Route("api/v{version:apiVersion}/product")]
[MustHavePermission(CatalogPermissions.Products.View)]
public class ProductController : VersionedApiController
{
    [HttpGet]
    public async Task<IActionResult> SearchAsync([FromQuery] ProductSearchRequest request)
    {
        return Ok(await Mediator.Send(new ListProductsQuery(request)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync([FromRoute] long id)
    {
        return Ok(await Mediator.Send(new GetProductByIdQuery(id)));
    }

    [HttpPut("{id?}")]
    [MustHavePermission(CatalogPermissions.Products.Manage)]
    public async Task<IActionResult> UpsertAsync([FromRoute] long? id, [FromBody] UpsertProductRequest request)
    {
        return Ok(await Mediator.Send(new UpsertProductCommand(id, request)));
    }

    [HttpPut("{id}/activate")]
    [MustHavePermission(CatalogPermissions.Products.Manage)]
    public async Task<IActionResult> ActivateAsync([FromRoute] long id)
    {
        return Ok(await Mediator.Send(new ActivateProductCommand(id)));
    }

    [HttpPut("{id}/deactivate")]
    [MustHavePermission(CatalogPermissions.Products.Manage)]
    public async Task<IActionResult> DeactivateAsync([FromRoute] long id)
    {
        return Ok(await Mediator.Send(new DeactivateProductCommand(id)));
    }

    [HttpPost("{id}/image")]
    [MustHavePermission(CatalogPermissions.Products.Manage)]
    public async Task<IActionResult> AddImageAsync([FromRoute] long id, [FromBody] AddProductImageRequest request)
    {
        return Ok(await Mediator.Send(new AddProductImageCommand(id, request)));
    }

    [HttpDelete("{id}/image")]
    [MustHavePermission(CatalogPermissions.Products.Manage)]
    public async Task<IActionResult> RemoveImageAsync([FromRoute] long id, [FromQuery] string url)
    {
        return Ok(await Mediator.Send(new RemoveProductImageCommand(id, url)));
    }

    [HttpDelete("{id}")]
    [MustHavePermission(CatalogPermissions.Products.Manage)]
    public async Task<IActionResult> DeleteAsync([FromRoute] long id)
    {
        return Ok(await Mediator.Send(new DeleteProductCommand(id)));
    }
}
