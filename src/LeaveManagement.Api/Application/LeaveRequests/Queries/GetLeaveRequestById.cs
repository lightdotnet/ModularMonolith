using Mapster;
using StarterKit.LeaveManagement.Api.Data;
using StarterKit.LeaveManagement.Api.Domain.LeaveRequests;

namespace StarterKit.LeaveManagement.Api.Application.LeaveRequests.Queries;

internal sealed record GetLeaveRequestByIdQuery(
    string Id,
    string? CurrentEmployeeId,
    bool CanManage) : IQuery<IResult<LeaveRequestDto>>;

internal class GetLeaveRequestByIdQueryHandler(
    LeaveManagementDbContext context)
    : IQueryHandler<GetLeaveRequestByIdQuery, IResult<LeaveRequestDto>>
{
    public async Task<IResult<LeaveRequestDto>> Handle(
        GetLeaveRequestByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await context.LeaveRequests
            .Where(new LeaveRequestByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result<LeaveRequestDto>.NotFound($"Leave request {request.Id} not found");

        if (!request.CanManage && entity.EmployeeId != request.CurrentEmployeeId)
            return Result<LeaveRequestDto>.Error("You can only view your own leave requests.");

        return Result<LeaveRequestDto>.Success(entity.Adapt<LeaveRequestDto>());
    }
}
