using StarterKit.Approval.Api.Data;
using StarterKit.Approval.Contracts.DocumentTypes;

namespace StarterKit.Approval.Api.Application.DocumentTypes.Commands;

internal sealed record UpdateApprovalDocumentTypeCommand(
    ApprovalDocumentTypeDto Model)
    : ICommand<IResult>;

internal class UpdateApprovalDocumentTypeCommandHandler(
    ApprovalDbContext context)
    : ICommandHandler<UpdateApprovalDocumentTypeCommand, IResult>
{
    public async Task<IResult> Handle(
        UpdateApprovalDocumentTypeCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        var entity = await context.ApprovalDocumentTypes
            .FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);

        if (entity is null)
            return Result.NotFound($"Approval document type {model.Id} not found");

        var codeTaken = await context.ApprovalDocumentTypes
            .AnyAsync(x => x.Id != model.Id && x.Code == model.Code, cancellationToken);

        if (codeTaken)
            return Result.Error($"A document type with code '{model.Code}' already exists.");

        entity.Name = model.Name;
        entity.Code = model.Code;
        entity.Description = model.Description;
        entity.IsActive = model.IsActive;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
