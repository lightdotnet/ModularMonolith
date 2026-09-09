using StarterKit.Approval.Contracts.Services;
using StarterKit.LeaveManagement.Api.Data;
using StarterKit.LeaveManagement.Api.Domain.LeaveRequests;

namespace StarterKit.LeaveManagement.Api.Application.LeaveRequests.Commands;

internal sealed record DeleteLeaveRequestCommand(
    string Id,
    string CurrentUserId,
    bool CanManage) : ICommand<IResult>;

internal class DeleteLeaveRequestCommandHandler(
    LeaveManagementDbContext context,
    IApprovalService approvalService)
    : ICommandHandler<DeleteLeaveRequestCommand, IResult>
{
    public async Task<IResult> Handle(
        DeleteLeaveRequestCommand request,
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
                return Result.Error("You can only delete your own leave requests.");

            if (entity.Status is not (LeaveRequestStatus.Pending or LeaveRequestStatus.Rejected))
                return Result.Error("This leave request can no longer be deleted.");
        }

        if (entity.Status == LeaveRequestStatus.Pending && entity.ApprovalRequestId is not null)
            await approvalService.CancelAsync(entity.ApprovalRequestId, entity.UserId, cancellationToken);

        context.LeaveRequests.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
