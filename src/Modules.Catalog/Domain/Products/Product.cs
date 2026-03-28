namespace Monolith.Catalog.Domain.Products;

public class Product : AuditableEntity
{
    public string ShopId { get; set; } = null!;

    public string? Code { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string CategoryId { get; set; } = null!;

    public string? MainImage { get; set; }

    public string Tags { get; set; } = null!;

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<ProductImage> Images { get; set; } = [];

    public static Product Create(string shopId, string categoryId, string name, string? description = null)
    {
        var product = new Product
        {
            ShopId = shopId,
            CategoryId = categoryId,
            Name = name,
            Description = description,
        };

        product.BuildTags();

        return product;
    }

    public void UpdateCode(string internalCode) => Code = internalCode;

    public void UpdateImages(IEnumerable<string> urls)
    {
        var productImages = urls.Select(s => new ProductImage
        {
            ProductId = Id,
            ImageUrl = s,
        });

        Images = [.. productImages];
    }

    public void BuildTags()
    {
        Tags = $"{Name} {Name.ToLower()} {Name.ToUpper()}";
    }
}
