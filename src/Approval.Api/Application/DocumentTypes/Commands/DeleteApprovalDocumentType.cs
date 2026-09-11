using StarterKit.Approval.Api.Data;
using StarterKit.Persistence.Extensions;

namespace StarterKit.Approval.Api.Application.DocumentTypes.Commands;

internal sealed record DeleteApprovalDocumentTypeCommand(string Id) : ICommand<IResult>;

file static class DeleteApprovalDocumentTypeMessages
{
    public const string InUse =
        "This document type is in use by one or more approval requests and cannot be deleted.";
}

internal class DeleteApprovalDocumentTypeCommandHandler(
    ApprovalDbContext context)
    : ICommandHandler<DeleteApprovalDocumentTypeCommand, IResult>
{
    public async Task<IResult> Handle(
        DeleteApprovalDocumentTypeCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.ApprovalDocumentTypes
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity is null)
            return Result.NotFound($"Approval document type {request.Id} not found");

        var inUse = await context.ApprovalRequests
            .AnyAsync(x => x.DocumentTypeId == request.Id, cancellationToken);

        if (inUse)
            return Result.Error(DeleteApprovalDocumentTypeMessages.InUse);

        context.ApprovalDocumentTypes.Remove(entity);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsForeignKeyConstraintViolation())
        {
            // A request referencing this type was created between the check above and the save.
            return Result.Error(DeleteApprovalDocumentTypeMessages.InUse);
        }

        return Result.Success();
    }
}
