using Light.Domain.ValueObjects;
using ValidationException = Light.Exceptions.ValidationException;

namespace StarterKit.Catalog.Api.Domain.Products;

/// <summary>
/// A single image URL attached to a product, with an optional display order. Mapped as an EF owned
/// collection (<c>OwnsMany</c>) into its own <c>ProductImages</c> table — unlike
/// <see cref="StarterKit.Shared.ValueObjects.Money"/>/<see cref="StarterKit.Shared.ValueObjects.VatPercentage"/>
/// (single table-split references, mutated in place via their own <c>Update</c>), a collection
/// member is always fully added or fully removed via <see cref="Product.AddImage"/>/
/// <see cref="Product.RemoveImage"/> — it never needs an in-place <c>Update</c>, so this type has no
/// mutator beyond construction.
/// </summary>
public sealed class ProductImageUrl : ValueObject
{
    // EF materialises the owned type through this parameterless constructor + the property
    // setters; stored rows are always already valid, so the guard is not re-run on read.
    private ProductImageUrl()
    {
    }

    public ProductImageUrl(
        string url,
        int? sortOrder = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw Invalid(nameof(url), "Image URL cannot be blank.");

        Url = url;
        SortOrder = sortOrder;
    }

    public string Url { get; private set; } = null!;

    public int? SortOrder { get; private set; }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Url;
        yield return SortOrder ?? -1;
    }

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
