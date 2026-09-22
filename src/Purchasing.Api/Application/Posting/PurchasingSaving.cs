using Light.Exceptions;
using StarterKit.Purchasing.Api.Data;

namespace StarterKit.Purchasing.Api.Application.Posting;

/// <summary>Shared save wrapper so an optimistic-concurrency loss surfaces as a 409, not a 500.</summary>
internal static class PurchasingSaving
{
    public const string ConcurrencyMessage =
        "The document was modified by another user; refresh and try again.";

    public static async Task SaveAsync(
        PurchasingDbContext context,
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
