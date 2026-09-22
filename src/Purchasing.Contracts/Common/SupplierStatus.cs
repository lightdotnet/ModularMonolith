namespace StarterKit.Purchasing.Contracts.Common;

public enum SupplierStatus
{
    Active = 0,

    /// <summary>Kept for history but cannot be used on new purchase orders.</summary>
    Inactive = 1,
}
