namespace StarterKit.Organization.Contracts.EmployeeLevels;

public record CreateEmployeeLevelRequest
{
    public string CompanyId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public int Rank { get; set; }

    public string? Description { get; set; }
}
