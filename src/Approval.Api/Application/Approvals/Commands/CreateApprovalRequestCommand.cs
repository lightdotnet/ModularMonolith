using FluentValidation;
using StarterKit.Approval.Contracts.Services;

namespace StarterKit.Approval.Api.Application.Approvals.Commands;

/// <summary>
/// Create-through-HTTP command (admin and self-service surfaces). Module-owned request types are
/// rejected here; owning modules create theirs in-process via <c>IApprovalService.CreateAsync</c>.
/// </summary>
internal sealed record CreateApprovalRequestCommand(
    CreateApprovalRequest Model) : ICommand<IResult<string>>;

internal sealed class CreateApprovalRequestCommandValidator : AbstractValidator<CreateApprovalRequestCommand>
{
    public CreateApprovalRequestCommandValidator()
    {
        RuleFor(x => x.Model.RequestType)
            .Must(type => !ApprovalRequestTypes.IsReserved(type))
            .WithMessage("This request type is reserved for the module that owns it and cannot be created here.");
    }
}

internal class CreateApprovalRequestCommandHandler(
    IApprovalService approvalService)
    : ICommandHandler<CreateApprovalRequestCommand, IResult<string>>
{
    public Task<IResult<string>> Handle(
        CreateApprovalRequestCommand request,
        CancellationToken cancellationToken) =>
        approvalService.CreateAsync(request.Model, cancellationToken);
}
