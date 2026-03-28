using Microsoft.AspNetCore.Mvc;
using Monolith.Catalog.UseCases.Categories.Commands;
using Monolith.Catalog.UseCases.Categories.Queries;

namespace Monolith.Catalog.Controllers;

public class CategoryController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok(await Mediator.Send(new GetCategoriesQuery()));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        return Ok(await Mediator.Send(new CreateCategoryCommand(request)));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] CategoryDto request)
    {
        return Ok(await Mediator.Send(new UpdateCategoryCommand(request)));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        return Ok(await Mediator.Send(new DeleteCategoryCommand(id)));
    }
}
