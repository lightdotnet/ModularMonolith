namespace StarterKit.Catalog.Api.Domain.Categories;

public class CategoryByIdSpec : Specification<Category>
{
    public CategoryByIdSpec(string categoryId)
    {
        Where(x => x.Id == categoryId);
    }
}
