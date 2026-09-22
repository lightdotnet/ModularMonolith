using Mapster;
using StarterKit.Catalog.Api.Data;
using StarterKit.Catalog.Api.Domain.Categories;

namespace StarterKit.Catalog.Api.Application.Categories.Queries;

internal sealed record GetCategoryByIdQuery(string Id) : IQuery<IResult<CategoryDto>>;

internal class GetCategoryByIdQueryHandler(CatalogDbContext context)
    : IQueryHandler<GetCategoryByIdQuery, IResult<CategoryDto>>
{
    public async Task<IResult<CategoryDto>> Handle(
        GetCategoryByIdQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await context.Categories
            .AsNoTracking()
            .Where(new CategoryByIdSpec(request.Id))
            .ProjectToType<CategoryDto>()
            .SingleOrDefaultAsync(cancellationToken);

        if (dto is null)
            return Result<CategoryDto>.NotFound($"Category {request.Id} not found");

        return Result<CategoryDto>.Success(dto);
    }
}
