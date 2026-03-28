using Monolith.Catalog.Domain.Products;

namespace Monolith.Catalog.Domain.Categories;

public class Category : AuditableEntity
{
    public string Name { get; set; } = null!;

    public bool Disable { get; set; }

    public string? SubOfId { get; set; }

    public virtual ICollection<Product> Products { get; set; } = [];

    public static Category Create(string name, string? subOfId = null)
    {
        return new Category
        {
            Name = name,
            SubOfId = subOfId
        };
    }

    public void Toggle() => Disable = !Disable;

    public bool IsMainCategory() => string.IsNullOrEmpty(SubOfId);
}
