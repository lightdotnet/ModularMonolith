using Microsoft.Extensions.Logging;
using StarterKit.Approval.Contracts.Services;
using StarterKit.Purchasing.Api.Application.Posting;
using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.PurchaseOrders;
using StarterKit.Shared;

namespace StarterKit.Purchasing.Api.Application.PurchaseOrders.Commands;

internal sealed record SubmitPurchaseOrderCommand(
    long Id,
    SubmitPurchaseOrderRequest Model,
    string CurrentUserId) : ICommand<IResult>;

internal sealed class SubmitPurchaseOrderCommandValidator : AbstractValidator<SubmitPurchaseOrderCommand>
{
    public SubmitPurchaseOrderCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.CurrentUserId).NotEmpty();
        RuleFor(x => x.Model).SetValidator(new SubmitPurchaseOrderRequestValidator());
    }
}

/// <summary>
/// Submits a draft — or resubmits a rejected — order for approval, exactly like a leave request
/// submission: the chosen approver is validated against the requester's approver candidates, the
/// Approval workflow is created <b>before</b> any local change (a resubmission always creates a brand-new
/// workflow; the rejected one is already finished), then one local commit records it. If that commit
/// fails the just-created workflow is cancelled on a best-effort basis so no orphan approval is left
/// behind. Only the requester can submit; the order's own guard runs first, so an order that cannot be
/// submitted never creates a workflow.
/// </summary>
internal class SubmitPurchaseOrderCommandHandler(
    PurchasingDbContext context,
    PurchaseOrderApprovalCoordinator coordinator,
    IApprovalService approvalService,
    IDateTime clock,
    ILogger<SubmitPurchaseOrderCommandHandler> logger)
    : ICommandHandler<SubmitPurchaseOrderCommand, IResult>
{
    public async Task<IResult> Handle(
        SubmitPurchaseOrderCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.PurchaseOrders
            .Include(x => x.Lines)
            .Where(new PurchaseOrderByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Purchase order {request.Id} not found");

        // Throws Forbidden/Conflict/Validation before anything is created in Approval.
        entity.EnsureCanSubmit(request.CurrentUserId);

        var approverResolution = await coordinator.ResolveApproverAsync(
            entity.RequesterEmployeeId,
            request.Model.ApproverEmployeeId,
            cancellationToken);

        if (!approverResolution.IsSuccess)
            return Result.Error(approverResolution.Message);

        var approver = approverResolution.Data;

        var approvalResult = await coordinator.CreateApprovalAsync(entity, approver, cancellationToken);

        if (!approvalResult.IsSuccess)
        {
            logger.LogWarning(
                "Approval refused to create a workflow for purchase order {PurchaseOrderId}: {Message}",
                entity.Id,
                approvalResult.Message);

            return Result.Error("Failed to submit the purchase order for approval.");
        }

        try
        {
            entity.Submit(
                request.CurrentUserId,
                approvalResult.Data,
                approver.EmployeeId,
                approver.Name,
                clock.UtcNow);

            await PurchasingSaving.SaveAsync(context, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to persist submission of purchase order {PurchaseOrderId} after approval {ApprovalRequestId} was created; attempting to cancel the workflow.",
                entity.Id,
                approvalResult.Data);

            try
            {
                await approvalService.CancelAsync(
                    approvalResult.Data,
                    entity.RequesterUserId,
                    cancellationToken);
            }
            catch (Exception cancelEx)
            {
                logger.LogError(
                    cancelEx,
                    "Failed to compensate by cancelling approval {ApprovalRequestId} for the unsaved purchase order submission.",
                    approvalResult.Data);
            }

            throw;
        }

        return Result.Success();
    }
}
