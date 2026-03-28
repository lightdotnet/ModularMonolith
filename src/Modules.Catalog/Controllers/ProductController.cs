using Microsoft.AspNetCore.Mvc;
using Monolith.Catalog.UseCases.Products.Queries;

namespace Monolith.Catalog.Controllers;

public class ProductController : ApiControllerBase
{
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] ProductLookup request)
    {
        return Ok(await Mediator.Send(new SearchProducts.Query(request)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBy(string id)
    {
        return Ok(await Mediator.Send(new GetProductById.Query(id)));
    }
}
