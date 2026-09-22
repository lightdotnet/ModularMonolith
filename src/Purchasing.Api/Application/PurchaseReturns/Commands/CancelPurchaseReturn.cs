using StarterKit.Purchasing.Api.Application.Posting;
using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.PurchaseReturns;
using StarterKit.Shared;

namespace StarterKit.Purchasing.Api.Application.PurchaseReturns.Commands;

internal sealed record CancelPurchaseReturnCommand(
    long Id,
    CancelPurchaseReturnRequest Model,
    string CurrentUserId) : ICommand<IResult>;

internal sealed class CancelPurchaseReturnCommandValidator : AbstractValidator<CancelPurchaseReturnCommand>
{
    public CancelPurchaseReturnCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Model).SetValidator(new CancelPurchaseReturnRequestValidator());
    }
}

internal class CancelPurchaseReturnCommandHandler(
    PurchasingDbContext context,
    IDateTime clock)
    : ICommandHandler<CancelPurchaseReturnCommand, IResult>
{
    public async Task<IResult> Handle(
        CancelPurchaseReturnCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.PurchaseReturns
            .Where(new PurchaseReturnByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Purchase return {request.Id} not found");

        entity.Cancel(
            request.Model.Reason,
            request.CurrentUserId,
            clock.UtcNow);

        await PurchasingSaving.SaveAsync(context, cancellationToken);

        return Result.Success();
    }
}
