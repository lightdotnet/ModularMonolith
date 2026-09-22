using Light.Domain.Entities;
using Light.Domain.Entities.Interfaces;
using Light.Exceptions;
using StarterKit.Shared.Entities;
using StarterKit.Shared.ValueObjects;

namespace StarterKit.Catalog.Api.Domain.Products;

/// <summary>
/// Aggregate root for a single sellable product: SKU, price, VAT rate, category assignment, and a
/// small gallery of image URLs. Every state transition runs through a guarded behaviour method —
/// <see cref="Create"/>, <see cref="Rename"/>, <see cref="UpdateDescription"/>, <see cref="Reprice"/>,
/// <see cref="UpdateVatRate"/>, <see cref="Recategorize"/>, <see cref="AddImage"/>/
/// <see cref="RemoveImage"/>, <see cref="Activate"/>/<see cref="Deactivate"/>, <see cref="ClearSku"/>,
/// <see cref="Delete"/> — rather than open setters. <see cref="Price"/> and <see cref="VatRate"/> are
/// mapped as EF owned types (table-split, same row): <see cref="Reprice"/>/<see cref="UpdateVatRate"/>
/// mutate the existing tracked instance in place via <see cref="Money.Update"/>/
/// <see cref="VatPercentage.Update"/> rather than reassigning — mirrors
/// <see cref="StarterKit.LeaveManagement.Api.Domain.LeaveRequests.LeaveRequest.ReviseDetails"/>'s
/// <c>Period.Update(...)</c> pattern (reassigning an owned reference makes EF's change tracker emit
/// the old instance as <c>Deleted</c>, which <c>TrackingExtensions.AuditEntries</c> resets back to
/// <c>Unchanged</c> to guard against nulling the owned columns — but that reset then leaves the new
/// values unpersisted). A <c>null</c> <see cref="Sku"/>/<see cref="Money"/>/<see cref="VatPercentage"/>
/// argument is a caller-programming-error, not a user-facing validation failure — every caller
/// always constructs the value object first (each guards its own shape at construction), so
/// <see cref="ArgumentNullException"/> fits better than a <c>ValidationException</c> here, same
/// reasoning as <see cref="StarterKit.LeaveManagement.Api.Domain.LeaveRequests.LeaveRequest.Create"/>'s
/// <c>period</c> guard.
/// <para>
/// <see cref="ISoftDelete"/> (<see cref="Deleted"/>/<see cref="DeletedBy"/>) is implemented explicitly
/// — set only by <c>TrackingExtensions.AuditEntries</c> through the interface reference, never
/// directly by application code — the first active soft-delete instance in this repo (see
/// <c>CatalogDbContext.ConfigureModel</c>'s <c>Product</c> query filter, the precedent for the next
/// soft-deletable aggregate).
/// </para>
/// </summary>
public class Product : AuditableEntity<long>, ISoftDelete
{
    private readonly List<ProductImageUrl> _images = [];

    private Product()
    {
    }

    public string CategoryId { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    /// <summary>
    /// <c>null</c> once <see cref="ClearSku"/> has been called — freeing the SKU for reuse by a new
    /// product. Set once at <see cref="Create"/> time and thereafter only ever moves toward
    /// <c>null</c>; there is no "change SKU" operation.
    /// </summary>
    public Sku? Sku { get; private set; }

    public Money Price { get; private set; } = null!;

    public VatPercentage VatRate { get; private set; } = null!;

    public ProductStatus Status { get; private set; } = ProductStatus.Active;

    public DateTimeOffset? Deleted { get; private set; }

    public string? DeletedBy { get; private set; }

    DateTimeOffset? ISoftDelete.Deleted
    {
        get => Deleted;
        set => Deleted = value;
    }

    string? ISoftDelete.DeletedBy
    {
        get => DeletedBy;
        set => DeletedBy = value;
    }

    public IReadOnlyList<ProductImageUrl> Images => _images.AsReadOnly();

    public static Product Create(
        string categoryId,
        string name,
        string? description,
        Sku sku,
        Money price,
        VatPercentage vatRate)
    {
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentNullException.ThrowIfNull(price);
        ArgumentNullException.ThrowIfNull(vatRate);

        return new Product
        {
            CategoryId = categoryId,
            Name = name,
            Description = description,
            Sku = sku,
            Price = price,
            VatRate = vatRate,
            Status = ProductStatus.Active,
        };
    }

    public void Rename(string name)
    {
        Name = name;
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
    }

    public void Reprice(Money price)
    {
        ArgumentNullException.ThrowIfNull(price);

        Price.Update(price.Amount, price.Currency);
    }

    public void UpdateVatRate(VatPercentage vatRate)
    {
        ArgumentNullException.ThrowIfNull(vatRate);

        VatRate.Update(vatRate.Value);
    }

    public void Recategorize(string categoryId)
    {
        CategoryId = categoryId;
    }

    public void AddImage(ProductImageUrl image)
    {
        ArgumentNullException.ThrowIfNull(image);

        _images.Add(image);
    }

    public void RemoveImage(string url)
    {
        _images.RemoveAll(x => x.Url == url);
    }

    public void RemoveImages()
    {
        _images.Clear();
    }

    public void Activate()
    {
        Status = ProductStatus.Active;
    }

    public void Deactivate()
    {
        Status = ProductStatus.Inactive;
    }

    /// <summary>Frees the SKU for reuse by a new product. Always safe — no invariant to guard.</summary>
    public void UpdateSku(string? sku)
    {
        if (string.IsNullOrEmpty(sku))
        {
            Sku = null;
        }
        else if (Sku?.Value != sku)
        {
            Sku = new Sku(sku);
        }
    }

    /// <summary>
    /// Guarded so a sellable product cannot vanish out from under an active catalog — the actual
    /// soft-delete (EF <c>Remove()</c> intercepted by <c>TrackingExtensions.AuditEntries</c> with
    /// <c>enableSoftDelete: true</c>) is triggered by the caller after this passes.
    /// </summary>
    public void Delete()
    {
        if (Status != ProductStatus.Inactive)
            throw new ConflictException("Only an inactive product can be deleted.");
    }
}
