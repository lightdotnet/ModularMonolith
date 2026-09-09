using Microsoft.Extensions.Logging;
using StarterKit.Approval.Contracts.Approvals;
using StarterKit.Approval.Contracts.Services;
using StarterKit.LeaveManagement.Api.Data;
using StarterKit.LeaveManagement.Api.Domain.LeaveRequests;
using StarterKit.Organization.Contracts.Services;

namespace StarterKit.LeaveManagement.Api.Application.LeaveRequests.Commands;

internal sealed record CreateLeaveRequestCommand(
    CreateLeaveRequest Model,
    string RequesterUserId,
    string? RequesterEmployeeId) : ICommand<IResult<string>>;

internal class CreateLeaveRequestCommandHandler(
    LeaveManagementDbContext context,
    IOrgDirectoryService orgDirectoryService,
    IApprovalService approvalService,
    ILogger<CreateLeaveRequestCommandHandler> logger)
    : ICommandHandler<CreateLeaveRequestCommand, IResult<string>>
{
    public async Task<IResult<string>> Handle(
        CreateLeaveRequestCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.RequesterEmployeeId))
            return Result<string>.Error("Your account is not linked to an employee record.");

        var model = request.Model;

        if (model.EndDate < model.StartDate)
            return Result<string>.Error("End date cannot be before start date.");

        var candidates = await orgDirectoryService.GetApproverCandidatesAsync(
            request.RequesterEmployeeId, cancellationToken);

        if (candidates.Count == 0)
            return Result<string>.Error("No approver could be determined for this employee's department.");

        var approver = candidates.FirstOrDefault(x => x.EmployeeId == model.ApproverEmployeeId);

        if (approver is null)
            return Result<string>.Error("Invalid approver selection.");

        // The JWT carries no name claims, so the requester's display name is resolved from their
        // Organization employee record instead — same source as the approver's own name above.
        var requesterName = await orgDirectoryService.GetEmployeeNameAsync(
            request.RequesterEmployeeId, cancellationToken);

        // Built in memory only — the approval workflow is created first so nothing is persisted
        // locally when it fails, and there is a single local commit once it succeeds.
        var entity = new LeaveRequest
        {
            UserId = request.RequesterUserId,
            EmployeeId = request.RequesterEmployeeId,
            LeaveType = model.LeaveType,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            Reason = model.Reason,
            Status = LeaveRequestStatus.Pending,
        };

        var approvalResult = await approvalService.CreateAsync(
            new CreateApprovalRequest(
                RequestType: LeaveRequestStatusMap.RequestType,
                RequestId: entity.Id,
                RequesterUserId: request.RequesterUserId,
                RequesterEmployeeId: request.RequesterEmployeeId,
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
            return Result<string>.Error(approvalResult.Message);

        entity.ApprovalRequestId = approvalResult.Data;

        try
        {
            await context.LeaveRequests.AddAsync(entity, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to persist leave request {LeaveRequestId} after approval {ApprovalRequestId} was created; attempting to cancel the workflow.",
                entity.Id,
                approvalResult.Data);

            try
            {
                await approvalService.CancelAsync(
                    approvalResult.Data,
                    request.RequesterUserId,
                    cancellationToken);
            }
            catch (Exception cancelEx)
            {
                logger.LogError(
                    cancelEx,
                    "Failed to compensate by cancelling approval {ApprovalRequestId} for the unsaved leave request.",
                    approvalResult.Data);
            }

            return Result<string>.Error("Could not save the leave request. Please try again.");
        }

        return Result<string>.Success(entity.Id);
    }
}
