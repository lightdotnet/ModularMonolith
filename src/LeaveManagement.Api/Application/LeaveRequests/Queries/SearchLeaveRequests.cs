using StarterKit.LeaveManagement.Api.Data;
using StarterKit.Persistence.Extensions;

namespace StarterKit.LeaveManagement.Api.Application.LeaveRequests.Queries;

internal sealed record SearchLeaveRequestsQuery(
    LeaveRequestSearchRequest Request,
    string CurrentUserId,
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
            : context.LeaveRequests.Where(x => x.UserId == request.CurrentUserId);

        if (request.CanManage && !string.IsNullOrEmpty(lookup.EmployeeId))
            scoped = scoped.Where(x => x.EmployeeId == lookup.EmployeeId);

        if (lookup.LeaveType.HasValue)
            scoped = scoped.Where(x => x.LeaveType == lookup.LeaveType!.Value);

        if (lookup.Status.HasValue)
            scoped = scoped.Where(x => x.Status == lookup.Status!.Value);

        // Hand-written projection instead of Mapster's ProjectToType: Mapster wraps nested member
        // access (src.Period.Start) in a null-propagation guard that EF Core cannot translate
        // against a required (non-nullable) owned type. Plain member access below translates fine.
        return await scoped
            .OrderByDescending(x => x.Created)
            .Select(x => new LeaveRequestDto
            {
                Id = x.Id,
                UserId = x.UserId,
                EmployeeId = x.EmployeeId,
                LeaveType = x.LeaveType,
                StartDate = x.Period.Start,
                EndDate = x.Period.End,
                Reason = x.Reason,
                Status = x.Status,
                ApprovalRequestId = x.ApprovalRequestId,
                Created = x.Created,
            })
            .ToPagedResultAsync(lookup, cancellationToken);
    }
}
