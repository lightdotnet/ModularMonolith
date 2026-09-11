using StarterKit.Approval.Contracts.Services;

namespace StarterKit.Approval.Api.Application.Approvals.Commands;

internal sealed record CreateApprovalRequestCommand(
    CreateApprovalRequest Model) : ICommand<IResult<string>>;

internal class CreateApprovalRequestCommandHandler(
    IApprovalService approvalService)
    : ICommandHandler<CreateApprovalRequestCommand, IResult<string>>
{
    public Task<IResult<string>> Handle(
        CreateApprovalRequestCommand request,
        CancellationToken cancellationToken) =>
        approvalService.CreateAsync(request.Model, cancellationToken);
}
