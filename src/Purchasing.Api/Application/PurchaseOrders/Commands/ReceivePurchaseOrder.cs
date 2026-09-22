using Light.Exceptions;
using Microsoft.Extensions.Logging;
using StarterKit.Inventory.Contracts.Services;
using StarterKit.Persistence.Extensions;
using StarterKit.Purchasing.Api.Application.Posting;
using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.GoodsReceipts;
using StarterKit.Purchasing.Api.Domain.PurchaseOrders;
using StarterKit.Shared;

namespace StarterKit.Purchasing.Api.Application.PurchaseOrders.Commands;

internal sealed record ReceivePurchaseOrderCommand(
    long Id,
    ReceivePurchaseOrderRequest Model,
    string ReceivedByUserId) : ICommand<IResult<long>>;

internal sealed class ReceivePurchaseOrderCommandValidator : AbstractValidator<ReceivePurchaseOrderCommand>
{
    public ReceivePurchaseOrderCommandValidator(IDateTime clock)
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.ReceivedByUserId).NotEmpty();
        RuleFor(x => x.Model).SetValidator(new ReceivePurchaseOrderRequestValidator());

        // Small tolerance for clock skew between the client and the server; the lower bound (not before
        // the order was approved) is a domain rule on the order.
        RuleFor(x => x.Model.ReceivedAt)
            .LessThanOrEqualTo(_ => clock.UtcNow.AddMinutes(5))
            .WithMessage("The received date cannot be in the future.");
    }
}

/// <summary>
/// Two-commit receive, like a transfer receipt. The goods receipt is recorded as <c>Posting</c> first
/// (so it has an id to post under, and so the insert touches the order's concurrency token, which is what
/// makes two racing receipts collide before either posts stock); the inbound stock is then received at
/// each line's cost, and a second commit marks the receipt posted and applies the quantities to the order
/// in one save. A delivery note reference that was already submitted — including a concurrent duplicate
/// that loses the insert race — resolves to the receipt already recorded for it, provided the requested
/// lines are identical (otherwise it is a conflict); a receipt still <c>Posting</c> is finished under its
/// original recorder, and the returned value is the receipt id. A lost concurrency race with a
/// <em>different</em> delivery is retried against the fresh state (bounded), so it either succeeds or is
/// rejected by the quantity guard. If Inventory deterministically refuses the posting the receipt is
/// voided and the refusal is rethrown.
/// </summary>
internal class ReceivePurchaseOrderCommandHandler(
    PurchasingDbContext context,
    IInventoryService inventoryService,
    IDateTime clock,
    ILogger<ReceivePurchaseOrderCommandHandler> logger)
    : ICommandHandler<ReceivePurchaseOrderCommand, IResult<long>>
{
    private const int MaxRecordAttempts = 3;

    public async Task<IResult<long>> Handle(
        ReceivePurchaseOrderCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        var deliveryNoteRef = string.IsNullOrWhiteSpace(model.DeliveryNoteRef)
            ? null
            : model.DeliveryNoteRef.Trim();

        var lines = model.Lines
            .Select(x => (x.PurchaseOrderLineId, x.Quantity))
            .ToList();

        GoodsReceipt? receipt = null;
        var lastFailureWasUniqueViolation = false;

        for (var attempt = 1; attempt <= MaxRecordAttempts && receipt is null; attempt++)
        {
            var order = await context.PurchaseOrders
                .Include(x => x.Lines)
                .Where(new PurchaseOrderByIdSpec(request.Id))
                .FirstOrDefaultAsync(cancellationToken);

            if (order is null)
                return Result<long>.NotFound($"Purchase order {request.Id} not found");

            if (deliveryNoteRef is not null)
            {
                var existing = await context.GoodsReceipts
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(
                        x => x.PurchaseOrderId == order.Id
                            && x.DeliveryNoteRef == deliveryNoteRef
                            && x.Status != GoodsReceiptStatus.Voided,
                        cancellationToken);

                if (existing is not null)
                {
                    EnsureSameLines(existing, lines);

                    receipt = existing;
                    break;
                }
            }

            var candidate = GoodsReceipt.Create(
                order,
                deliveryNoteRef,
                model.ReceivedAt,
                request.ReceivedByUserId,
                lines,
                await context.InFlightQuantitiesAsync(order.Id, cancellationToken),
                clock.UtcNow);

            await context.GoodsReceipts.AddAsync(candidate, cancellationToken);

            try
            {
                await context.SaveChangesAsync(cancellationToken);

                receipt = candidate;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Lost the race on the order's token: start over from fresh state, which re-validates the
                // quantities against whatever the winner recorded.
                lastFailureWasUniqueViolation = false;

                context.ChangeTracker.Clear();
            }
            catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
            {
                // Lost the race on the delivery-note index (the next pass resolves to the winning
                // receipt) or, rarely, collided on the generated receipt number (the next pass generates
                // a new one).
                lastFailureWasUniqueViolation = true;

                context.ChangeTracker.Clear();
            }
        }

        if (receipt is null)
            throw new ConflictException(
                lastFailureWasUniqueViolation
                    ? "Could not allocate a unique receipt number; please try again."
                    : PurchasingSaving.ConcurrencyMessage);

        if (receipt.Status == GoodsReceiptStatus.Posted)
            return Result<long>.Success(receipt.Id);

        var outcome = await PurchasingPosting.ReceiveAsync(
            context,
            inventoryService,
            receipt,
            clock,
            postingsAlreadyLanded: false,
            logger,
            cancellationToken);

        // Refused: the receipt was voided and Inventory's validation error is rethrown.
        if (outcome.Failure is not null)
            throw outcome.Failure;

        return Result<long>.Success(receipt.Id);
    }

    /// <summary>A replayed delivery note must describe the same delivery, whatever the line order.</summary>
    private static void EnsureSameLines(
        GoodsReceipt existing,
        IReadOnlyCollection<(long PurchaseOrderLineId, int Quantity)> requested)
    {
        var stored = existing.Lines
            .Select(x => (x.PurchaseOrderLineId, x.Quantity))
            .OrderBy(x => x.PurchaseOrderLineId)
            .ToList();

        var incoming = requested
            .OrderBy(x => x.PurchaseOrderLineId)
            .ToList();

        if (!stored.SequenceEqual(incoming))
            throw new ConflictException("This delivery note was already recorded with different lines.");
    }
}
