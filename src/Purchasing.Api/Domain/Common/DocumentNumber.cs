using System.Security.Cryptography;

namespace StarterKit.Purchasing.Api.Domain.Common;

/// <summary>
/// Generator shared by <c>PurchaseOrderNumber</c>, <c>GoodsReceiptNumber</c> and
/// <c>PurchaseReturnNumber</c>, modeled on <c>StarterKit.Transfers.Api.Domain.StockTransfers.TransferCode</c>:
/// a two-letter document prefix + <c>yyyyMMdd</c> + a 9-character Crockford Base32 suffix (no
/// <c>I</c>/<c>L</c>/<c>O</c>/<c>U</c>, to avoid misreads).
/// </summary>
internal static class DocumentNumber
{
    public const int MaxLength = 20;

    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    private const int RandomSuffixLength = 9;

    public static string Generate(
        string prefix,
        DateTimeOffset now) =>
        $"{prefix}{now:yyyyMMdd}{RandomSuffix(RandomSuffixLength)}";

    private static string RandomSuffix(int length)
    {
        Span<char> buffer = stackalloc char[length];

        for (var i = 0; i < length; i++)
            buffer[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];

        return new string(buffer);
    }
}
