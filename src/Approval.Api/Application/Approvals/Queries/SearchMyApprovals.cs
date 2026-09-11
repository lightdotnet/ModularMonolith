using Light.EntityFrameworkCore.Extensions;
using Mapster;
using StarterKit.Approval.Api.Data;
using StarterKit.Persistence.Extensions;

namespace StarterKit.Approval.Api.Application.Approvals.Queries;

internal sealed record SearchMyApprovalsQuery(
    string UserId,
    MyApprovalRequestSearchRequest Request)
    : IQuery<PagedResult<ApprovalRequestDto>>;

internal class SearchMyApprovalsQueryHandler(
    ApprovalDbContext context)
    : IQueryHandler<SearchMyApprovalsQuery, PagedResult<ApprovalRequestDto>>
{
    public Task<PagedResult<ApprovalRequestDto>> Handle(
        SearchMyApprovalsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = request.UserId;
        var lookup = request.Request;

        var query = context.ApprovalRequests.AsNoTracking();

        query = lookup.Relation switch
        {
            ApprovalRelation.Requested => query.Where(x => x.RequesterUserId == userId),
            ApprovalRelation.AwaitingMyDecision => query.Where(x =>
                x.Status == ApprovalStatus.Pending
                && x.Steps.Any(s => s.Level == x.CurrentLevel
                    && s.ApproverUserId == userId
                    && s.Status == ApprovalStepStatus.Pending)),
            ApprovalRelation.DecidedByMe => query.Where(x =>
                x.Steps.Any(s => s.ApproverUserId == userId && s.DecidedAt != null)),
            _ => query.Where(x =>
                x.RequesterUserId == userId || x.Steps.Any(s => s.ApproverUserId == userId)),
        };

        return query
            .WhereIf(!string.IsNullOrEmpty(lookup.RequestType), x => x.RequestType == lookup.RequestType)
            .WhereIf(lookup.Status.HasValue, x => x.Status == lookup.Status!.Value)
            .WhereIf(!string.IsNullOrEmpty(lookup.SearchValue), x => x.Title.Contains(lookup.SearchValue!))
            .OrderByDescending(x => x.Created)
            .ProjectToType<ApprovalRequestDto>()
            .ToPagedResultAsync(lookup, cancellationToken);
    }
}
