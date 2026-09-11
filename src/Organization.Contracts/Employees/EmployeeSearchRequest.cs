namespace StarterKit.Organization.Contracts.Employees;

public record EmployeeSearchRequest : SearchQuery
{
    public string? CompanyId { get; set; }

    public string? OrgUnitId { get; set; }

    public EmploymentStatus? EmploymentStatus { get; set; }

    /// <summary>
    /// When true, only employees that have a linked Identity user account are returned.
    /// </summary>
    public bool? LinkedToUserOnly { get; set; }
}
