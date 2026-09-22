namespace StarterKit.Purchasing.Contracts.Common;

/// <summary>
/// Input bounds shared by the request validators. They also keep line totals (unit cost times quantity,
/// summed over an order) far inside the range of <see cref="decimal"/>.
/// </summary>
public static class PurchasingLimits
{
    public const int MaxQuantity = 1_000_000;

    public const decimal MaxAmount = 1_000_000_000m;

    public const int MaxLines = 200;
}
