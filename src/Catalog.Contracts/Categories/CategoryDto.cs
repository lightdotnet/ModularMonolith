namespace StarterKit.Catalog.Contracts.Categories;

public class CategoryDto : BaseDto
{
    public string? ParentCategoryId { get; set; }

    public string Name { get; set; } = null!;
}
