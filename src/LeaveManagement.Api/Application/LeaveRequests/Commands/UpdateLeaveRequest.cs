using StarterKit.Approval.Contracts.Approvals;
using StarterKit.Approval.Contracts.Services;
using StarterKit.LeaveManagement.Api.Data;
using StarterKit.LeaveManagement.Api.Domain.LeaveRequests;
using StarterKit.Organization.Contracts.Services;

namespace StarterKit.LeaveManagement.Api.Application.LeaveRequests.Commands;

/// <summary>
/// Editing a <c>Pending</c>/<c>Rejected</c> request (the only statuses a non-management caller may
/// touch) is treated as a resubmission: Approval has no "update" primitive, only Create/Decide/
/// Cancel/GetByRequest, so the prior (still-pending) approval request is cancelled and a fresh one
/// is created against the edited fields and a freshly-resolved approver. A <c>.manage</c> edit is a
/// metadata correction only and never touches the approval workflow.
/// </summary>
internal sealed record UpdateLeaveRequestCommand(
    string Id,
    UpdateLeaveRequest Model,
    string CurrentUserId,
    bool CanManage) : ICommand<IResult>;

internal class UpdateLeaveRequestCommandHandler(
    LeaveManagementDbContext context,
    IOrgDirectoryService orgDirectoryService,
    IApprovalService approvalService)
    : ICommandHandler<UpdateLeaveRequestCommand, IResult>
{
    public async Task<IResult> Handle(
        UpdateLeaveRequestCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.LeaveRequests
            .Where(new LeaveRequestByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Leave request {request.Id} not found");

        if (!request.CanManage)
        {
            if (entity.UserId != request.CurrentUserId)
                return Result.Error("You can only edit your own leave requests.");

            if (entity.Status is not (LeaveRequestStatus.Pending or LeaveRequestStatus.Rejected))
                return Result.Error("This leave request can no longer be edited.");
        }

        var model = request.Model;

        if (model.EndDate < model.StartDate)
            return Result.Error("End date cannot be before start date.");

        var wasPending = entity.Status == LeaveRequestStatus.Pending;

        entity.LeaveType = model.LeaveType;
        entity.StartDate = model.StartDate;
        entity.EndDate = model.EndDate;
        entity.Reason = model.Reason;

        if (!request.CanManage)
        {
            if (string.IsNullOrEmpty(model.ApproverEmployeeId))
                return Result.Error("Please select an approver.");

            if (wasPending && entity.ApprovalRequestId is not null)
                await approvalService.CancelAsync(entity.ApprovalRequestId, entity.UserId, cancellationToken);

            var candidates = await orgDirectoryService.GetApproverCandidatesAsync(
                entity.EmployeeId, cancellationToken);

            var approver = candidates.FirstOrDefault(x => x.EmployeeId == model.ApproverEmployeeId);

            if (approver is null)
                return Result.Error("Invalid approver selection.");

            // The JWT carries no name claims, so the requester's display name is resolved from
            // their Organization employee record instead — same source as the approver's own name.
            var requesterName = await orgDirectoryService.GetEmployeeNameAsync(
                entity.EmployeeId, cancellationToken);

            var approvalResult = await approvalService.CreateAsync(
                new CreateApprovalRequest(
                    RequestType: "LeaveRequest",
                    RequestId: entity.Id,
                    RequesterUserId: entity.UserId,
                    RequesterEmployeeId: entity.EmployeeId,
                    RequesterName: requesterName,
                    Title: $"{model.LeaveType} leave request",
                    Content: model.Reason,
                    DeepLinkUrl: $"/leave-requests/{entity.Id}",
                    DocumentTypeId: null,
                    ApproverChain:
                    [
                        new ApproverStepInput(1, approver.UserId, approver.EmployeeId, approver.Name),
                    ]),
                cancellationToken);

            if (!approvalResult.IsSuccess)
                return Result.Error("Failed to resubmit the leave request for approval.");

            entity.ApprovalRequestId = approvalResult.Data;
            entity.Status = LeaveRequestStatus.Pending;
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
