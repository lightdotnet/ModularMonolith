using Mapster;
using Microsoft.AspNetCore.Mvc;
using Monolith.Catalog.Domain.Shops;

namespace Monolith.Catalog.Controllers;

public class ShopController : ApiControllerBase
{
    [HttpPost("search")]
    public async Task<IActionResult> Get([FromBody] ShopLookup request)
    {
        var res = await Context.Set<Shop>()
            .WhereIf(!string.IsNullOrEmpty(request.Search), x => x.Name.Contains(request.Search!))
            .AsNoTracking()
            .ProjectToType<ShopDto>()
            .ToPagedAsync(request);

        return Ok(res);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get([FromRoute] string id)
    {
        var res = await Context.Set<Shop>()
            .Where(new ShopByIdSpec(id))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        return Ok(res);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateShopRequest request)
    {
        var entity = Shop.Create(request.Name);

        await Context.Set<Shop>().AddAsync(entity);
        await Context.SaveChangesAsync();

        return Ok();
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] ShopDto request)
    {
        var record = await Context.Set<Shop>()
            .Where(new ShopByIdSpec(request.Id))
            .FirstOrDefaultAsync();

        if (record == null)
        {
            return Ok(Result.NotFound($"Shop ID: {request.Id} not found."));
        }

        record.Name = request.Name;
        record.Status.Update(request.Status);

        await Context.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var record = await Context.Set<Shop>()
            .Where(new ShopByIdSpec(id))
            .FirstOrDefaultAsync();

        if (record == null)
        {
            return Ok(Result.NotFound($"Shop ID: {id} not found."));
        }

        if (record.Status.IsUnactive is false)
        {
            return Ok(Result.Error("Only shops with 'Unactive' status can be deleted."));
        }

        Context.Set<Shop>().Remove(record);

        await Context.SaveChangesAsync();

        return Ok();
    }
}
