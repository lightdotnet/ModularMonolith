using Light.Exceptions;
using Microsoft.Extensions.Logging;
using StarterKit.Inventory.Contracts.Services;
using StarterKit.Purchasing.Api.Application.Posting;
using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.PurchaseReturns;
using StarterKit.Shared;

namespace StarterKit.Purchasing.Api.Application.PurchaseReturns.Commands;

internal sealed record PostPurchaseReturnCommand(
    long Id,
    string PostedByUserId) : ICommand<IResult>;

internal sealed class PostPurchaseReturnCommandValidator : AbstractValidator<PostPurchaseReturnCommand>
{
    public PostPurchaseReturnCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.PostedByUserId).NotEmpty();
    }
}

/// <summary>
/// There is no shared transaction with Inventory, and the stock issue needs the return's id, so the
/// return is committed first as <c>Posting</c>, then stock is issued at the receiving location
/// (all-or-nothing, strict no-oversell), then a second commit records the cost removed, the expected
/// credit and the informational returned quantities, and moves it to <c>Posted</c>. An Inventory refusal
/// returns the return to Draft; a crash in between leaves it in <c>Posting</c> for
/// <see cref="PurchasingPostingReconciliationService"/> to finish (see <see cref="PurchasingPosting"/>).
/// </summary>
internal class PostPurchaseReturnCommandHandler(
    PurchasingDbContext context,
    IInventoryService inventoryService,
    IDateTime clock,
    ILogger<PostPurchaseReturnCommandHandler> logger)
    : ICommandHandler<PostPurchaseReturnCommand, IResult>
{
    public async Task<IResult> Handle(
        PostPurchaseReturnCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.PurchaseReturns
            .Include(x => x.Lines)
            .Where(new PurchaseReturnByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Purchase return {request.Id} not found");

        entity.BeginPost(request.PostedByUserId, clock.UtcNow);

        PostingOutcome outcome;

        try
        {
            await context.SaveChangesAsync(cancellationToken);

            outcome = await PurchasingPosting.PostReturnAsync(
                context,
                inventoryService,
                entity,
                clock,
                logger,
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(PurchasingSaving.ConcurrencyMessage);
        }

        // Refused: the return is back in Draft. Pending: a transient Inventory error with nothing landed,
        // so the return stays Posting for the sweep to retry. Either way the caller sees Inventory's own
        // error.
        if (outcome.Failure is not null)
            throw outcome.Failure;

        return Result.Success();
    }
}
