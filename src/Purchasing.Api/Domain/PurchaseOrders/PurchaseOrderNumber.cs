using StarterKit.Purchasing.Api.Domain.Common;
using ValidationException = Light.Exceptions.ValidationException;

namespace StarterKit.Purchasing.Api.Domain.PurchaseOrders;

/// <summary>
/// A unique, human-readable reference for a <c>PurchaseOrder</c>, modeled on Transfers' <c>TransferCode</c>: a plain
/// sealed class with manual equality, mapped as a converted scalar column with a unique index. Always
/// system-generated (<c>PO</c> + date + random suffix, see <see cref="DocumentNumber"/>).
/// </summary>
public sealed class PurchaseOrderNumber
{
    public const int MaxLength = DocumentNumber.MaxLength;

    private const string Prefix = "PO";

    public PurchaseOrderNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Invalid(nameof(value), "Purchase order number cannot be blank.");

        value = value.Trim();

        if (value.Length > MaxLength)
            throw Invalid(nameof(value), $"Purchase order number cannot exceed {MaxLength} characters.");

        Value = value;
    }

    public string Value { get; }

    public static PurchaseOrderNumber Generate(DateTimeOffset now) => new(DocumentNumber.Generate(Prefix, now));

    public override bool Equals(object? obj) =>
        obj is PurchaseOrderNumber other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
