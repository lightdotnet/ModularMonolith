using Light.Exceptions;
using StarterKit.Inventory.Contracts.Services;
using StarterKit.Persistence.Extensions;
using StarterKit.Shared;
using StarterKit.Transfers.Api.Data;
using StarterKit.Transfers.Api.Domain.StockTransfers;

namespace StarterKit.Transfers.Api.Application.StockTransfers.Commands;

internal sealed record ReceiveStockTransferCommand(
    long Id,
    ReceiveStockTransferRequest Model,
    string ReceivedByUserId) : ICommand<IResult<long>>;

internal sealed class ReceiveStockTransferCommandValidator : AbstractValidator<ReceiveStockTransferCommand>
{
    public ReceiveStockTransferCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.ReceivedByUserId).NotEmpty();
        RuleFor(x => x.Model).SetValidator(new ReceiveStockTransferRequestValidator());
    }
}

/// <summary>
/// Same two-commit shape as dispatch: the receipt is recorded as <c>Posting</c> first (so it has an id
/// to post under), the inbound stock is received at the destination at the frozen dispatch cost, then
/// a second commit applies the quantities. Resubmitting the same <c>ClientRequestId</c> — including a
/// concurrent duplicate that loses the insert race — finds the existing receipt and only finishes it
/// if it is still <c>Posting</c>; the returned value is the receipt id. If Inventory deterministically
/// refuses the posting the receipt is voided and the refusal is rethrown.
/// </summary>
internal class ReceiveStockTransferCommandHandler(
    TransfersDbContext context,
    IInventoryService inventoryService,
    IDateTime clock)
    : ICommandHandler<ReceiveStockTransferCommand, IResult<long>>
{
    public async Task<IResult<long>> Handle(
        ReceiveStockTransferCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await LoadAsync(request.Id, cancellationToken);

        if (entity is null)
            return Result<long>.NotFound($"Transfer {request.Id} not found");

        var model = request.Model;

        var receipt = entity.BeginReceive(
            model.ClientRequestId,
            model.Lines
                .Select(x => (x.TransferLineId, x.Quantity))
                .ToList(),
            clock.UtcNow);

        PostingOutcome outcome;

        try
        {
            // A brand-new receipt has no id until it is committed.
            if (receipt.Id == 0)
            {
                try
                {
                    await context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex) when (ex is DbUpdateConcurrencyException
                    || (ex is DbUpdateException db && db.IsUniqueConstraintViolation()))
                {
                    // A concurrent submit of the same ClientRequestId won (on the parent's concurrency
                    // token or on the unique index): return the receipt it recorded instead of a 409.
                    context.ChangeTracker.Clear();

                    entity = await LoadAsync(request.Id, cancellationToken);

                    receipt = entity?.FindReceiptByClientRequestId(model.ClientRequestId)
                        ?? throw new ConflictException(TransferSaving.ConcurrencyMessage);
                }
            }

            if (receipt.Status == TransferReceiptStatus.Posted)
                return Result<long>.Success(receipt.Id);

            if (receipt.Status == TransferReceiptStatus.Voided)
                throw new ConflictException(
                    "This receipt was voided because Inventory refused it; submit a new receipt with a new client request id.");

            outcome = await TransferPosting.ReceiveAsync(
                context,
                inventoryService,
                entity!,
                receipt,
                request.ReceivedByUserId,
                clock,
                postingsAlreadyLanded: false,
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(TransferSaving.ConcurrencyMessage);
        }

        // Refused: the receipt was voided and Inventory's validation error is rethrown.
        if (outcome.Failure is not null)
            throw outcome.Failure;

        return Result<long>.Success(receipt.Id);
    }

    private Task<StockTransfer?> LoadAsync(
        long id,
        CancellationToken cancellationToken) =>
        context.StockTransfers
            .Include(x => x.Lines)
            .Include(x => x.Receipts)
                .ThenInclude(x => x.Lines)
            .Where(new StockTransferByIdSpec(id))
            .FirstOrDefaultAsync(cancellationToken);
}
