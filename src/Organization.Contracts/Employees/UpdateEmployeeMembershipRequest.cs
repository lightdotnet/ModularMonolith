namespace StarterKit.Organization.Contracts.Employees;

public record UpdateEmployeeMembershipRequest
{
    public string? LevelId { get; set; }

    public bool IsPrimary { get; set; }

    public AssignmentType AssignmentType { get; set; } = AssignmentType.Current;

    public bool IsManager { get; set; }
}
