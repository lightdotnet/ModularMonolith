using Mapster;
using StarterKit.Catalog.Api.Data;

namespace StarterKit.Catalog.Api.Application.Categories.Queries;

internal sealed record GetCategoryChildrenQuery(string Id) : IQuery<IList<CategoryDto>>;

internal class GetCategoryChildrenQueryHandler(CatalogDbContext context)
    : IQueryHandler<GetCategoryChildrenQuery, IList<CategoryDto>>
{
    public async Task<IList<CategoryDto>> Handle(
        GetCategoryChildrenQuery request,
        CancellationToken cancellationToken) =>
        await context.Categories
            .AsNoTracking()
            .Where(x => x.ParentCategoryId == request.Id)
            .ProjectToType<CategoryDto>()
            .ToListAsync(cancellationToken);
}
