using StarterKit.Shared.Entities;

namespace StarterKit.Organization.Api.Domain.Employees;

public class EmployeeLevel : AuditableEntity
{
    public string CompanyId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public int Rank { get; set; }

    public string? Description { get; set; }
}
