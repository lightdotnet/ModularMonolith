using StarterKit.Catalog.Contracts.Common;

namespace StarterKit.Catalog.Contracts.Products;

public record ProductSearchRequest : SearchQuery
{
    public string? CategoryId { get; set; }

    public ProductStatus? Status { get; set; }
}
