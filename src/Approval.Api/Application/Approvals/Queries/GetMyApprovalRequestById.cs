using Mapster;
using StarterKit.Approval.Api.Data;
using StarterKit.Approval.Api.Domain.Approvals;

namespace StarterKit.Approval.Api.Application.Approvals.Queries;

internal sealed record GetMyApprovalRequestByIdQuery(
    string Id,
    string UserId)
    : IQuery<IResult<ApprovalRequestDto>>;

internal class GetMyApprovalRequestByIdQueryHandler(
    ApprovalDbContext context)
    : IQueryHandler<GetMyApprovalRequestByIdQuery, IResult<ApprovalRequestDto>>
{
    public async Task<IResult<ApprovalRequestDto>> Handle(
        GetMyApprovalRequestByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await context.ApprovalRequests
            .AsNoTracking()
            .Include(x => x.Steps)
            // Required for the .Adapt() flatten below to populate ApprovalRequestDto.DocumentTypeName;
            // the sibling ProjectToType handlers get the join for free and need no Include.
            .Include(x => x.DocumentType)
            .Where(new ApprovalRequestByIdSpec(request.Id))
            .SingleOrDefaultAsync(cancellationToken);

        // Not found and "not yours" resolve to the same NotFound response, so a user can't
        // probe for the existence of a request they have no relation to.
        var isRelated = entity is not null
            && (entity.RequesterUserId == request.UserId
                || entity.Steps.Any(s => s.ApproverUserId == request.UserId));

        if (!isRelated)
            return Result<ApprovalRequestDto>.NotFound($"Approval request {request.Id} not found");

        return Result<ApprovalRequestDto>.Success(entity!.Adapt<ApprovalRequestDto>());
    }
}
