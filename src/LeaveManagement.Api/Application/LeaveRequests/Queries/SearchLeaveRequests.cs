using Mapster;
using StarterKit.LeaveManagement.Api.Data;
using StarterKit.Persistence.Extensions;

namespace StarterKit.LeaveManagement.Api.Application.LeaveRequests.Queries;

internal sealed record SearchLeaveRequestsQuery(
    LeaveRequestSearchRequest Request,
    string? CurrentEmployeeId,
    bool CanManage) : IQuery<PagedResult<LeaveRequestDto>>;

internal class SearchLeaveRequestsQueryHandler(
    LeaveManagementDbContext context)
    : IQueryHandler<SearchLeaveRequestsQuery, PagedResult<LeaveRequestDto>>
{
    public async Task<PagedResult<LeaveRequestDto>> Handle(
        SearchLeaveRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var lookup = request.Request;

        var scoped = request.CanManage
            ? context.LeaveRequests.AsQueryable()
            : context.LeaveRequests.Where(x => x.EmployeeId == request.CurrentEmployeeId);

        if (request.CanManage && !string.IsNullOrEmpty(lookup.EmployeeId))
            scoped = scoped.Where(x => x.EmployeeId == lookup.EmployeeId);

        if (lookup.LeaveType.HasValue)
            scoped = scoped.Where(x => x.LeaveType == lookup.LeaveType!.Value);

        if (lookup.Status.HasValue)
            scoped = scoped.Where(x => x.Status == lookup.Status!.Value);

        return await scoped
            .OrderByDescending(x => x.Created)
            .ProjectToType<LeaveRequestDto>()
            .ToPagedResultAsync(lookup, cancellationToken);
    }
}
