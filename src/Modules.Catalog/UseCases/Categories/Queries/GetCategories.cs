namespace Monolith.Catalog.UseCases.Categories.Queries;

public record GetCategoriesQuery : IQuery<IEnumerable<CategoryVm>>;

internal class GetCategoriesQueryHandler(CatalogContext context)
    : IQueryHandler<GetCategoriesQuery, IEnumerable<CategoryVm>>
{
    public async Task<IEnumerable<CategoryVm>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await context.Set<Category>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return categories
            .Where(x => string.IsNullOrEmpty(x.SubOfId))
            .Select(s => new CategoryVm
            {
                Id = s.Id,
                Name = s.Name,
                Disable = s.Disable,
                SubCategories = categories
                    .Where(x => x.SubOfId == s.Id)
                    .Select(sub => new CategoryDto
                    {
                        Id = sub.Id,
                        Name = sub.Name,
                        Disable = sub.Disable
                    })
            });
    }
}
