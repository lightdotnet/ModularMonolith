namespace StarterKit.Organization.Contracts.Services;

/// <summary>
/// Cross-module seam for resolving "who approves for this employee" — used by LeaveManagement
/// (and any future module needing an org-hierarchy-based approver) without exposing OrgUnit/
/// EmployeeOrgUnitMembership internals outside Organization.
/// </summary>
public interface IOrgDirectoryService
{
    /// <summary>
    /// Resolves every eligible approver candidate for <paramref name="employeeId"/> in their
    /// primary org unit: every active <c>IsManager == true</c> member if any exist, otherwise
    /// every active member — ordered by <c>EmployeeLevel.Rank</c> descending (most senior first).
    /// Excludes the employee themselves and any candidate without a linked Identity login (they
    /// could never act on an approval step). Climbs to the parent org unit only when the current
    /// one has no eligible candidate at all, until the hierarchy is exhausted. Returns an empty
    /// list if the employee has no active primary membership, or no candidate can be found
    /// anywhere up the chain.
    /// </summary>
    Task<IReadOnlyList<ResolvedApproverDto>> GetApproverCandidatesAsync(
        string employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves an employee's display name (<c>FirstName + " " + LastName</c>), or <c>null</c> if
    /// no employee with that id exists. For a cosmetic label only — never an identity check.
    /// </summary>
    Task<string?> GetEmployeeNameAsync(string employeeId, CancellationToken cancellationToken = default);
}
