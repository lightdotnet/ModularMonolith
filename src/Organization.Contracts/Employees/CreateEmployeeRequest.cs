namespace StarterKit.Organization.Contracts.Employees;

public record CreateEmployeeRequest
{
    public string CompanyId { get; set; } = null!;

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

    public string? AvatarUrl { get; set; }
}
