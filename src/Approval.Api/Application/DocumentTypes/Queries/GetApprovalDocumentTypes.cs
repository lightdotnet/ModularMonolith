using Mapster;
using StarterKit.Approval.Api.Data;
using StarterKit.Approval.Contracts.DocumentTypes;
using StarterKit.Persistence.Extensions;

namespace StarterKit.Approval.Api.Application.DocumentTypes.Queries;

internal sealed record GetApprovalDocumentTypesQuery(
    bool? ActiveOnly = null)
    : IQuery<IList<ApprovalDocumentTypeDto>>;

internal class GetApprovalDocumentTypesQueryHandler(
    ApprovalDbContext context)
    : IQueryHandler<GetApprovalDocumentTypesQuery, IList<ApprovalDocumentTypeDto>>
{
    public async Task<IList<ApprovalDocumentTypeDto>> Handle(
        GetApprovalDocumentTypesQuery request,
        CancellationToken cancellationToken)
    {
        return await context.ApprovalDocumentTypes
            .AsNoTracking()
            .WhereIf(request.ActiveOnly == true, x => x.IsActive)
            .OrderBy(x => x.Name)
            .ProjectToType<ApprovalDocumentTypeDto>()
            .ToListAsync(cancellationToken);
    }
}
