using StarterKit.Catalog.Api.Data;

namespace StarterKit.Catalog.Api.Application.Categories.Queries;

internal sealed record GetCategoryTreeQuery : IQuery<IList<CategoryTreeNodeDto>>;

internal class GetCategoryTreeQueryHandler(CatalogDbContext context)
    : IQueryHandler<GetCategoryTreeQuery, IList<CategoryTreeNodeDto>>
{
    public async Task<IList<CategoryTreeNodeDto>> Handle(
        GetCategoryTreeQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await context.Categories
            .AsNoTracking()
            .Select(x => new CategoryTreeNodeDto
            {
                Id = x.Id,
                ParentCategoryId = x.ParentCategoryId,
                Name = x.Name,
            })
            .ToListAsync(cancellationToken);

        var byParent = categories
            .GroupBy(x => x.ParentCategoryId ?? string.Empty)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var category in categories)
        {
            if (byParent.TryGetValue(category.Id, out var children))
                category.Children = children;
        }

        return byParent.TryGetValue(string.Empty, out var roots) ? roots : [];
    }
}
