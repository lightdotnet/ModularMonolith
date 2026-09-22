using System.Security.Cryptography;
using ValidationException = Light.Exceptions.ValidationException;

namespace StarterKit.Transfers.Api.Domain.StockTransfers;

/// <summary>
/// A unique, human-readable reference for a <see cref="StockTransfer"/>, modeled on
/// <c>StarterKit.Orders.Api.Domain.Orders.OrderCode</c>: a plain sealed class with manual equality,
/// mapped as a converted scalar column with a unique index. Always system-generated:
/// <c>T</c> + <c>yyyyMMdd</c> + a 9-character Crockford Base32 suffix (no <c>I</c>/<c>L</c>/<c>O</c>/<c>U</c>,
/// to avoid misreads).
/// </summary>
public sealed class TransferCode
{
    public const int MaxLength = 20;

    private const string Prefix = "T";

    // Crockford Base32: excludes I/L/O/U to avoid visual misreads.
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    private const int RandomSuffixLength = 9;

    public TransferCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Invalid(nameof(value), "Transfer code cannot be blank.");

        value = value.Trim();

        if (value.Length > MaxLength)
            throw Invalid(nameof(value), $"Transfer code cannot exceed {MaxLength} characters.");

        Value = value;
    }

    public string Value { get; }

    public static TransferCode Generate(DateTimeOffset now) =>
        new($"{Prefix}{now:yyyyMMdd}{RandomSuffix(RandomSuffixLength)}");

    private static string RandomSuffix(int length)
    {
        Span<char> buffer = stackalloc char[length];

        for (var i = 0; i < length; i++)
            buffer[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];

        return new string(buffer);
    }

    public override bool Equals(object? obj) =>
        obj is TransferCode other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
