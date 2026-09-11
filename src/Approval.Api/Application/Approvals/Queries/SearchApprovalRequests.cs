using Mapster;
using StarterKit.Approval.Api.Data;
using StarterKit.Persistence.Extensions;

namespace StarterKit.Approval.Api.Application.Approvals.Queries;

internal sealed record SearchApprovalRequestsQuery(
    ApprovalRequestSearchRequest Request)
    : IQuery<PagedResult<ApprovalRequestDto>>;

internal class SearchApprovalRequestsQueryHandler(
    ApprovalDbContext context)
    : IQueryHandler<SearchApprovalRequestsQuery, PagedResult<ApprovalRequestDto>>
{
    public Task<PagedResult<ApprovalRequestDto>> Handle(
        SearchApprovalRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var lookup = request.Request;

        return context.ApprovalRequests
            .AsNoTracking()
            .WhereIf(!string.IsNullOrEmpty(lookup.RequestType), x => x.RequestType == lookup.RequestType)
            .WhereIf(lookup.Status.HasValue, x => x.Status == lookup.Status!.Value)
            .WhereIf(!string.IsNullOrEmpty(lookup.SearchValue), x => x.Title.Contains(lookup.SearchValue!))
            .OrderByDescending(x => x.Created)
            .ProjectToType<ApprovalRequestDto>()
            .ToPagedResultAsync(lookup, cancellationToken);
    }
}
