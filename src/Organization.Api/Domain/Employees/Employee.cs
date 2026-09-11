using StarterKit.Organization.Api.Domain.OrgUnits;
using StarterKit.Shared.Entities;

namespace StarterKit.Organization.Api.Domain.Employees;

public class Employee : AuditableEntity
{
    public string CompanyId { get; set; } = null!;

    public string? UserId { get; set; }

    public string EmployeeCode { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public DateTimeOffset? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? NationalId { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public DateTimeOffset? HireDate { get; set; }

    public DateTimeOffset? TerminationDate { get; set; }

    public EmploymentStatus EmploymentStatus { get; set; } = EmploymentStatus.Active;

    public string? AvatarUrl { get; set; }

    public IList<EmployeeOrgUnitMembership> Memberships { get; set; } = [];
}
