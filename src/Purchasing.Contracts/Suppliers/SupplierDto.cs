using StarterKit.Purchasing.Contracts.Common;

namespace StarterKit.Purchasing.Contracts.Suppliers;

public class SupplierDto : BaseDto<long>
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? ContactName { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? PaymentTerms { get; set; }

    public SupplierStatus Status { get; set; }
}
