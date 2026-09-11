using System.ComponentModel.DataAnnotations;

namespace StarterKit.Organization.Contracts.Employees;

public record CreateEmployeeLoginRequest
{
    public string UserName { get; set; } = null!;

    public string? Password { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }
}
