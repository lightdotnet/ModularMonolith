namespace StarterKit.Catalog.Contracts.Products;

public class ProductImageDto
{
    public string Url { get; set; } = null!;

    public int? SortOrder { get; set; }
}
