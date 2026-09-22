using Microsoft.Extensions.Logging;
using StarterKit.Persistence.Extensions;
using StarterKit.Purchasing.Api.Application.Posting;
using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.GoodsReceipts;
using StarterKit.Purchasing.Api.Domain.PurchaseReturns;
using StarterKit.Shared;

namespace StarterKit.Purchasing.Api.Application.PurchaseReturns.Commands;

internal sealed record CreatePurchaseReturnCommand(CreatePurchaseReturnRequest Model) : ICommand<IResult<long>>;

internal sealed class CreatePurchaseReturnCommandValidator : AbstractValidator<CreatePurchaseReturnCommand>
{
    public CreatePurchaseReturnCommandValidator()
    {
        RuleFor(x => x.Model).SetValidator(new CreatePurchaseReturnRequestValidator());
    }
}

/// <summary>
/// Creates a draft return against a posted goods receipt. The handler loads what the receipt's other
/// non-cancelled returns already claim and hands it to the aggregate, which enforces the per-line
/// over-return rule; the insert also touches the receipt's concurrency token, so two racing returns of the
/// same receipt collide instead of both passing that rule. The return number is always generated, so a
/// unique-index collision is retried with a fresh number (bounded).
/// </summary>
internal class CreatePurchaseReturnCommandHandler(
    PurchasingDbContext context,
    IDateTime clock,
    ILogger<CreatePurchaseReturnCommandHandler> logger)
    : ICommandHandler<CreatePurchaseReturnCommand, IResult<long>>
{
    private const int MaxGenerateAttempts = 3;

    public async Task<IResult<long>> Handle(
        CreatePurchaseReturnCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        var receipt = await context.GoodsReceipts
            .Include(x => x.Lines)
            .Where(new GoodsReceiptByIdSpec(model.GoodsReceiptId))
            .FirstOrDefaultAsync(cancellationToken);

        if (receipt is null)
            return Result<long>.NotFound($"Goods receipt {model.GoodsReceiptId} not found");

        var entity = PurchaseReturn.Create(
            receipt,
            model.Reason,
            model.Note,
            model.Lines
                .Select(x => (x.GoodsReceiptLineId, x.Quantity, x.Reason))
                .ToList(),
            await context.AlreadyReturnedAsync(receipt.Id, excludePurchaseReturnId: null, cancellationToken),
            clock.UtcNow);

        await context.PurchaseReturns.AddAsync(entity, cancellationToken);

        for (var attempt = 1; attempt <= MaxGenerateAttempts; attempt++)
        {
            try
            {
                await context.SaveChangesAsync(cancellationToken);
                return Result<long>.Success(entity.Id);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new Light.Exceptions.ConflictException(PurchasingSaving.ConcurrencyMessage);
            }
            catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation() && attempt < MaxGenerateAttempts)
            {
                logger.LogWarning(
                    ex,
                    "Generated return number collided on attempt {Attempt} of {MaxAttempts}; regenerating.",
                    attempt,
                    MaxGenerateAttempts);

                entity.RegenerateReturnNumber(clock.UtcNow);
            }
        }

        // Unreachable in practice: the final attempt either returns (success) or throws.
        throw new InvalidOperationException("Unreachable: CreatePurchaseReturnCommandHandler retry loop exhausted without returning or throwing.");
    }
}
