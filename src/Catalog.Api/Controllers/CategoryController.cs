using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Catalog.Api.Application.Categories.Commands;
using StarterKit.Catalog.Api.Application.Categories.Queries;
using StarterKit.Catalog.Contracts.Authorization;
using StarterKit.Infrastructure.Endpoints;

namespace StarterKit.Catalog.Api.Controllers;

[ApiExplorerSettings(GroupName = "catalog")]
[Route("api/v{version:apiVersion}/category")]
[MustHavePermission(CatalogPermissions.Categories.View)]
public class CategoryController : VersionedApiController
{
    [HttpGet("tree")]
    public async Task<IActionResult> GetTreeAsync()
    {
        return Ok(await Mediator.Send(new GetCategoryTreeQuery()));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(new GetCategoryByIdQuery(id)));
    }

    [HttpGet("{id}/children")]
    public async Task<IActionResult> GetChildrenAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(new GetCategoryChildrenQuery(id)));
    }

    [HttpPost]
    [MustHavePermission(CatalogPermissions.Categories.Manage)]
    public async Task<IActionResult> PostAsync([FromBody] CreateCategoryRequest request)
    {
        return Ok(await Mediator.Send(new CreateCategoryCommand(request)));
    }

    [HttpPut("{id}")]
    [MustHavePermission(CatalogPermissions.Categories.Manage)]
    public async Task<IActionResult> PutAsync([FromRoute] string id, [FromBody] UpdateCategoryRequest request)
    {
        return Ok(await Mediator.Send(new UpdateCategoryCommand(id, request)));
    }

    [HttpPut("{id}/move")]
    [MustHavePermission(CatalogPermissions.Categories.Manage)]
    public async Task<IActionResult> MoveAsync([FromRoute] string id, [FromBody] MoveCategoryRequest request)
    {
        return Ok(await Mediator.Send(new MoveCategoryCommand(id, request)));
    }

    [HttpDelete("{id}")]
    [MustHavePermission(CatalogPermissions.Categories.Manage)]
    public async Task<IActionResult> DeleteAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(new DeleteCategoryCommand(id)));
    }
}
