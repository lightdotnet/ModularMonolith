namespace Monolith.Catalog.Domain.Products;

public class ProductImage : AuditableEntity
{
    public string ProductId { get; set; } = null!;

    public string ImageUrl { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
