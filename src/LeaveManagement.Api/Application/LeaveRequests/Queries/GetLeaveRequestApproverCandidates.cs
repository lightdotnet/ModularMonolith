using StarterKit.Organization.Contracts.Services;

namespace StarterKit.LeaveManagement.Api.Application.LeaveRequests.Queries;

internal sealed record GetLeaveRequestApproverCandidatesQuery(
    string? CurrentEmployeeId) : IQuery<IResult<List<ApproverCandidateDto>>>;

internal class GetLeaveRequestApproverCandidatesQueryHandler(IOrgDirectoryService orgDirectoryService)
    : IQueryHandler<GetLeaveRequestApproverCandidatesQuery, IResult<List<ApproverCandidateDto>>>
{
    public async Task<IResult<List<ApproverCandidateDto>>> Handle(
        GetLeaveRequestApproverCandidatesQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.CurrentEmployeeId))
            return Result<List<ApproverCandidateDto>>.Error("Your account is not linked to an employee record.");

        var candidates = await orgDirectoryService.GetApproverCandidatesAsync(
            request.CurrentEmployeeId, cancellationToken);

        var dtos = candidates
            .Select(x => new ApproverCandidateDto
            {
                EmployeeId = x.EmployeeId,
                UserId = x.UserId,
                Name = x.Name,
            })
            .ToList();

        return Result<List<ApproverCandidateDto>>.Success(dtos);
    }
}
