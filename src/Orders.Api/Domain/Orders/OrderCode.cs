using System.Security.Cryptography;
using ValidationException = Light.Exceptions.ValidationException;

namespace StarterKit.Orders.Api.Domain.Orders;

/// <summary>
/// A unique, human-readable reference for an <see cref="Order"/> — modeled on
/// <see cref="StarterKit.Catalog.Api.Domain.Products.Sku"/> (plain sealed class, manual
/// <c>Equals</c>/<c>GetHashCode</c>/<c>ToString</c>, self-validating ctor — not
/// <c>Light.Domain.ValueObjects.ValueObject</c>, for the same "needs a direct equality-predicate
/// query" reason documented there). Mapped as a converted scalar column (<c>HasConversion</c>) with
/// a real unique index in <c>OrdersDbContext</c>, same treatment as <c>Sku</c>.
/// <para>
/// Two distinct shapes share this type: <see cref="Generate"/> always produces the specific
/// <c>yyyyMMdd</c> + 9-char Crockford Base32 system format (no <c>I</c>/<c>L</c>/<c>O</c>/<c>U</c>,
/// to avoid misreads); the public <see cref="OrderCode(string)"/> constructor is intentionally
/// looser (non-blank, within the column's max width) because a caller can also supply their own
/// code via <c>CreateOrderRequest.OrderCode</c> and that value is not guaranteed to follow the
/// generated shape.
/// </para>
/// </summary>
public sealed class OrderCode
{
    public const int MaxLength = 17;

    // Crockford Base32: excludes I/L/O/U to avoid visual misreads.
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    private const int RandomSuffixLength = 9;

    public OrderCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Invalid(nameof(value), "Order code cannot be blank.");

        value = value.Trim();

        if (value.Length > MaxLength)
            throw Invalid(nameof(value), $"Order code cannot exceed {MaxLength} characters.");

        Value = value;
    }

    public string Value { get; }

    /// <summary>The only path that produces the well-formed <c>yyyyMMdd</c> + 9-char system shape.</summary>
    public static OrderCode Generate(DateTimeOffset now) => new($"{now:yyyyMMdd}{RandomSuffix(RandomSuffixLength)}");

    private static string RandomSuffix(int length)
    {
        Span<char> buffer = stackalloc char[length];

        for (var i = 0; i < length; i++)
            buffer[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];

        return new string(buffer);
    }

    public override bool Equals(object? obj) =>
        obj is OrderCode other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
