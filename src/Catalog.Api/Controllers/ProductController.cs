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
    public async Task<IActionResult> GetAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(new GetProductByIdQuery(id)));
    }

    [HttpPost]
    [MustHavePermission(CatalogPermissions.Products.Manage)]
    public async Task<IActionResult> PostAsync([FromBody] CreateProductRequest request)
    {
        return Ok(await Mediator.Send(new CreateProductCommand(request)));
    }

    [HttpPut("{id}")]
    [MustHavePermission(CatalogPermissions.Products.Manage)]
    public async Task<IActionResult> PutAsync([FromRoute] string id, [FromBody] UpdateProductRequest request)
    {
        return Ok(await Mediator.Send(new UpdateProductCommand(id, request)));
    }

    [HttpPut("{id}/activate")]
    [MustHavePermission(CatalogPermissions.Products.Manage)]
    public async Task<IActionResult> ActivateAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(new ActivateProductCommand(id)));
    }

    [HttpPut("{id}/deactivate")]
    [MustHavePermission(CatalogPermissions.Products.Manage)]
    public async Task<IActionResult> DeactivateAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(new DeactivateProductCommand(id)));
    }

    [HttpPost("{id}/image")]
    [MustHavePermission(CatalogPermissions.Products.Manage)]
    public async Task<IActionResult> AddImageAsync([FromRoute] string id, [FromBody] AddProductImageRequest request)
    {
        return Ok(await Mediator.Send(new AddProductImageCommand(id, request)));
    }

    [HttpDelete("{id}/image")]
    [MustHavePermission(CatalogPermissions.Products.Manage)]
    public async Task<IActionResult> RemoveImageAsync([FromRoute] string id, [FromQuery] string url)
    {
        return Ok(await Mediator.Send(new RemoveProductImageCommand(id, url)));
    }
}
