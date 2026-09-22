using StarterKit.Shared.Entities;

namespace StarterKit.Purchasing.Api.Domain.Suppliers;

/// <summary>
/// A vendor goods are bought from. A plain entity — it has no lifecycle beyond being
/// <see cref="SupplierStatus.Active"/> or <see cref="SupplierStatus.Inactive"/>; only an active supplier
/// can be put on a new purchase order (checked by the handler that creates the order). The code is
/// normalized (trimmed, upper-cased) so uniqueness is case-insensitive in practice. Required/length
/// checks on the incoming values are FluentValidation's job, not the entity's.
/// </summary>
public class Supplier : AuditableEntity<long>
{
    private Supplier()
    {
    }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? ContactName { get; private set; }

    public string? Phone { get; private set; }

    public string? Email { get; private set; }

    public string? Address { get; private set; }

    /// <summary>Free-text payment terms (for example "Net 30").</summary>
    public string? PaymentTerms { get; private set; }

    public SupplierStatus Status { get; private set; } = SupplierStatus.Active;

    public bool IsActive => Status == SupplierStatus.Active;

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    public static Supplier Create(
        string code,
        string name,
        string? contactName,
        string? phone,
        string? email,
        string? address,
        string? paymentTerms)
    {
        var supplier = new Supplier
        {
            Status = SupplierStatus.Active,
        };

        supplier.Update(
            code,
            name,
            contactName,
            phone,
            email,
            address,
            paymentTerms);

        return supplier;
    }

    public void Update(
        string code,
        string name,
        string? contactName,
        string? phone,
        string? email,
        string? address,
        string? paymentTerms)
    {
        Code = NormalizeCode(code);
        Name = name.Trim();
        ContactName = contactName;
        Phone = phone;
        Email = email;
        Address = address;
        PaymentTerms = paymentTerms;
    }

    public void Activate() => Status = SupplierStatus.Active;

    public void Deactivate() => Status = SupplierStatus.Inactive;
}
