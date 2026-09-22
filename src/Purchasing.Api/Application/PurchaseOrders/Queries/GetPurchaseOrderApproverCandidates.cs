using StarterKit.Organization.Contracts.Services;

namespace StarterKit.Purchasing.Api.Application.PurchaseOrders.Queries;

internal sealed record GetPurchaseOrderApproverCandidatesQuery(
    string? CurrentEmployeeId) : IQuery<IResult<List<ApproverCandidateDto>>>;

/// <summary>The caller's own approver candidates, for the approver picker shown before submitting.</summary>
internal class GetPurchaseOrderApproverCandidatesQueryHandler(IOrgDirectoryService orgDirectoryService)
    : IQueryHandler<GetPurchaseOrderApproverCandidatesQuery, IResult<List<ApproverCandidateDto>>>
{
    public async Task<IResult<List<ApproverCandidateDto>>> Handle(
        GetPurchaseOrderApproverCandidatesQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.CurrentEmployeeId))
            return Result<List<ApproverCandidateDto>>.Error("Your account is not linked to an employee record.");

        var candidates = await orgDirectoryService.GetApproverCandidatesAsync(
            request.CurrentEmployeeId,
            cancellationToken);

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
