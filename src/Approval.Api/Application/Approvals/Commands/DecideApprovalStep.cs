using StarterKit.Approval.Contracts.Services;

namespace StarterKit.Approval.Api.Application.Approvals.Commands;

internal sealed record DecideApprovalStepCommand(
    string ApprovalRequestId,
    string DecidedByUserId,
    bool Approved,
    string? Comment) : ICommand<IResult>;

internal class DecideApprovalStepCommandHandler(
    IApprovalService approvalService)
    : ICommandHandler<DecideApprovalStepCommand, IResult>
{
    public Task<IResult> Handle(
        DecideApprovalStepCommand request,
        CancellationToken cancellationToken) =>
        approvalService.DecideAsync(
            request.ApprovalRequestId,
            request.DecidedByUserId,
            request.Approved,
            request.Comment,
            cancellationToken);
}
