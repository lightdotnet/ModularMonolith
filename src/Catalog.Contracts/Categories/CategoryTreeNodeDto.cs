namespace StarterKit.Catalog.Contracts.Categories;

public class CategoryTreeNodeDto : BaseDto
{
    public string? ParentCategoryId { get; set; }

    public string Name { get; set; } = null!;

    public IList<CategoryTreeNodeDto> Children { get; set; } = [];
}
