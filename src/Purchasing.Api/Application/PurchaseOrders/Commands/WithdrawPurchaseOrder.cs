using StarterKit.Approval.Contracts.Approvals;
using StarterKit.Approval.Contracts.Services;
using StarterKit.Purchasing.Api.Application.Posting;
using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.PurchaseOrders;
using StarterKit.Shared;

namespace StarterKit.Purchasing.Api.Application.PurchaseOrders.Commands;

internal sealed record WithdrawPurchaseOrderCommand(
    long Id,
    string CurrentUserId) : ICommand<IResult>;

internal sealed class WithdrawPurchaseOrderCommandValidator : AbstractValidator<WithdrawPurchaseOrderCommand>
{
    public WithdrawPurchaseOrderCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.CurrentUserId).NotEmpty();
    }
}

/// <summary>
/// Requester pulls a pending submission back to Draft. The Approval workflow is cancelled first; if that
/// fails the workflow's real status is looked up. An already-cancelled workflow (an earlier withdraw whose
/// local commit failed) lets the withdrawal complete, and an already-decided one is applied to the order
/// instead, so the caller sees the decision rather than a misleading generic error.
/// </summary>
internal class WithdrawPurchaseOrderCommandHandler(
    PurchasingDbContext context,
    IApprovalService approvalService,
    IDateTime clock)
    : ICommandHandler<WithdrawPurchaseOrderCommand, IResult>
{
    public async Task<IResult> Handle(
        WithdrawPurchaseOrderCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.PurchaseOrders
            .Where(new PurchaseOrderByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Purchase order {request.Id} not found");

        entity.EnsureCanWithdraw(request.CurrentUserId);

        var approvalRequestId = entity.ApprovalRequestId!;

        var cancel = await approvalService.CancelAsync(
            approvalRequestId,
            entity.RequesterUserId,
            cancellationToken);

        if (!cancel.IsSuccess)
        {
            var view = await approvalService.GetStatusAsync(
                approvalRequestId,
                cancellationToken);

            if (view is null
                || view.ApprovalRequestId != approvalRequestId
                || view.Status != ApprovalStatus.Cancelled)
            {
                if (view is not null)
                {
                    // Decided in the meantime: apply the decision so the order reflects it.
                    await PurchaseOrderApprovalCoordinator.ApplyOutcomeAsync(
                        entity,
                        context,
                        view,
                        clock.UtcNow,
                        cancellationToken);
                }

                return Result.Conflict("This purchase order can no longer be withdrawn; it may already have been decided.");
            }
        }

        entity.Withdraw(request.CurrentUserId);

        await PurchasingSaving.SaveAsync(context, cancellationToken);

        return Result.Success();
    }
}
