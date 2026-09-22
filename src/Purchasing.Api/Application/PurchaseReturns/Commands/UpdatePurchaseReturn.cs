using StarterKit.Purchasing.Api.Application.Posting;
using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.PurchaseReturns;

namespace StarterKit.Purchasing.Api.Application.PurchaseReturns.Commands;

internal sealed record UpdatePurchaseReturnCommand(
    long Id,
    UpdatePurchaseReturnRequest Model) : ICommand<IResult>;

internal sealed class UpdatePurchaseReturnCommandValidator : AbstractValidator<UpdatePurchaseReturnCommand>
{
    public UpdatePurchaseReturnCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Model).SetValidator(new UpdatePurchaseReturnRequestValidator());
    }
}

/// <summary>
/// Replaces a draft return's reason, note and lines. What the receipt's <em>other</em> non-cancelled
/// returns already claim is passed to the aggregate, which enforces the over-return rule; the touched
/// return and lines also rotate the receipt's concurrency token (see <c>PurchasingDbContext</c>).
/// </summary>
internal class UpdatePurchaseReturnCommandHandler(PurchasingDbContext context)
    : ICommandHandler<UpdatePurchaseReturnCommand, IResult>
{
    public async Task<IResult> Handle(
        UpdatePurchaseReturnCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.PurchaseReturns
            .Include(x => x.Lines)
            .Include(x => x.GoodsReceipt)
                .ThenInclude(x => x.Lines)
            .Where(new PurchaseReturnByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Purchase return {request.Id} not found");

        var model = request.Model;

        entity.UpdateDraft(
            entity.GoodsReceipt,
            model.Reason,
            model.Note,
            model.Lines
                .Select(x => (x.GoodsReceiptLineId, x.Quantity, x.Reason))
                .ToList(),
            await context.AlreadyReturnedAsync(entity.GoodsReceiptId, entity.Id, cancellationToken));

        await PurchasingSaving.SaveAsync(context, cancellationToken);

        return Result.Success();
    }
}
