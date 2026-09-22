using Light.Exceptions;
using StarterKit.Transfers.Api.Data;

namespace StarterKit.Transfers.Api.Application.StockTransfers;

/// <summary>Shared save wrapper so an optimistic-concurrency loss surfaces as a 409, not a 500.</summary>
internal static class TransferSaving
{
    public const string ConcurrencyMessage =
        "The transfer was modified by another user; refresh and try again.";

    public static async Task SaveAsync(
        TransfersDbContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(ConcurrencyMessage);
        }
    }
}
