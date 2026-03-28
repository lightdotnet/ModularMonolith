using System.ComponentModel.DataAnnotations;

namespace Monolith.Identity;

public class CreateUserRequest
{
    public string? UserName { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Password { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? AuthProvider { get; set; }
}