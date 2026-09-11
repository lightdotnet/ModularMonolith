using Mapster;
using StarterKit.Approval.Api.Data;
using StarterKit.Approval.Contracts.DocumentTypes;

namespace StarterKit.Approval.Api.Application.DocumentTypes.Queries;

internal sealed record GetApprovalDocumentTypeByIdQuery(string Id)
    : IQuery<IResult<ApprovalDocumentTypeDto>>;

internal class GetApprovalDocumentTypeByIdQueryHandler(
    ApprovalDbContext context)
    : IQueryHandler<GetApprovalDocumentTypeByIdQuery, IResult<ApprovalDocumentTypeDto>>
{
    public async Task<IResult<ApprovalDocumentTypeDto>> Handle(
        GetApprovalDocumentTypeByIdQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await context.ApprovalDocumentTypes
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .ProjectToType<ApprovalDocumentTypeDto>()
            .SingleOrDefaultAsync(cancellationToken);

        if (dto is null)
            return Result<ApprovalDocumentTypeDto>.NotFound($"Approval document type {request.Id} not found");

        return Result<ApprovalDocumentTypeDto>.Success(dto);
    }
}
