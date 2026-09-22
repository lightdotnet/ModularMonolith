using Light.Exceptions;
using StarterKit.Inventory.Contracts.Services;
using StarterKit.Shared;
using StarterKit.Transfers.Api.Data;
using StarterKit.Transfers.Api.Domain.StockTransfers;

namespace StarterKit.Transfers.Api.Application.StockTransfers.Commands;

internal sealed record DispatchStockTransferCommand(
    long Id,
    string DispatchedByUserId) : ICommand<IResult>;

internal sealed class DispatchStockTransferCommandValidator : AbstractValidator<DispatchStockTransferCommand>
{
    public DispatchStockTransferCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.DispatchedByUserId).NotEmpty();
    }
}

/// <summary>
/// There is no shared transaction with Inventory, and the stock issue needs the transfer's id, so the
/// transfer is committed first as <c>Posting</c>, then stock is issued at the source (all-or-nothing,
/// strict no-oversell), then a second commit records the costs and moves it to <c>Dispatched</c>. An
/// Inventory refusal returns the transfer to Draft; a crash in between leaves it in <c>Posting</c> for
/// <see cref="TransfersPostingReconciliationService"/> to finish (see <see cref="TransferPosting"/>).
/// </summary>
internal class DispatchStockTransferCommandHandler(
    TransfersDbContext context,
    IInventoryService inventoryService,
    IDateTime clock)
    : ICommandHandler<DispatchStockTransferCommand, IResult>
{
    public async Task<IResult> Handle(
        DispatchStockTransferCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.StockTransfers
            .Include(x => x.Lines)
            .Where(new StockTransferByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Transfer {request.Id} not found");

        entity.BeginDispatch(clock.UtcNow);

        PostingOutcome outcome;

        try
        {
            await context.SaveChangesAsync(cancellationToken);

            outcome = await TransferPosting.DispatchAsync(
                context,
                inventoryService,
                entity,
                request.DispatchedByUserId,
                clock,
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(TransferSaving.ConcurrencyMessage);
        }

        // Refused: the transfer is back in Draft. Pending: a transient Inventory error with nothing
        // landed, so the transfer stays Posting for the sweep to retry. Either way the caller sees
        // Inventory's own error.
        if (outcome.Failure is not null)
            throw outcome.Failure;

        return Result.Success();
    }
}
