using StarterKit.Purchasing.Contracts.Common;

namespace StarterKit.Purchasing.Contracts.Suppliers;

/// <summary><see cref="SearchQuery.SearchValue"/> matches the supplier code or name.</summary>
public record SearchSupplierRequest : SearchQuery
{
    public SupplierStatus? Status { get; set; }
}
