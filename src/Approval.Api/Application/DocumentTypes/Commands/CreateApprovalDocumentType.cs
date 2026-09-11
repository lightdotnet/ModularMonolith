using StarterKit.Approval.Api.Data;
using StarterKit.Approval.Api.Domain.Approvals;
using StarterKit.Approval.Contracts.DocumentTypes;

namespace StarterKit.Approval.Api.Application.DocumentTypes.Commands;

internal sealed record CreateApprovalDocumentTypeCommand(
    CreateApprovalDocumentTypeRequest Model)
    : ICommand<IResult<string>>;

internal class CreateApprovalDocumentTypeCommandHandler(
    ApprovalDbContext context)
    : ICommandHandler<CreateApprovalDocumentTypeCommand, IResult<string>>
{
    public async Task<IResult<string>> Handle(
        CreateApprovalDocumentTypeCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        var codeExists = await context.ApprovalDocumentTypes
            .AnyAsync(x => x.Code == model.Code, cancellationToken);

        if (codeExists)
            return Result<string>.Error($"A document type with code '{model.Code}' already exists.");

        var entity = new ApprovalDocumentType
        {
            Name = model.Name,
            Code = model.Code,
            Description = model.Description,
            IsActive = model.IsActive,
        };

        await context.ApprovalDocumentTypes.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result<string>.Success(entity.Id);
    }
}
