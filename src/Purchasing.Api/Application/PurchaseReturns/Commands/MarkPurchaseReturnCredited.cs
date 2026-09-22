using StarterKit.Purchasing.Api.Application.Posting;
using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.PurchaseReturns;
using StarterKit.Shared;

namespace StarterKit.Purchasing.Api.Application.PurchaseReturns.Commands;

internal sealed record MarkPurchaseReturnCreditedCommand(
    long Id,
    MarkPurchaseReturnCreditedRequest Model,
    string CurrentUserId) : ICommand<IResult>;

internal sealed class MarkPurchaseReturnCreditedCommandValidator : AbstractValidator<MarkPurchaseReturnCreditedCommand>
{
    public MarkPurchaseReturnCreditedCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Model).SetValidator(new MarkPurchaseReturnCreditedRequestValidator());
    }
}

/// <summary>Records the supplier credit note against a posted return; bookkeeping only, nothing is gated on it.</summary>
internal class MarkPurchaseReturnCreditedCommandHandler(
    PurchasingDbContext context,
    IDateTime clock)
    : ICommandHandler<MarkPurchaseReturnCreditedCommand, IResult>
{
    public async Task<IResult> Handle(
        MarkPurchaseReturnCreditedCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.PurchaseReturns
            .Where(new PurchaseReturnByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Purchase return {request.Id} not found");

        entity.MarkCredited(
            request.Model.CreditNoteNumber.Trim(),
            request.Model.CreditAmount,
            request.CurrentUserId,
            clock.UtcNow);

        await PurchasingSaving.SaveAsync(context, cancellationToken);

        return Result.Success();
    }
}
