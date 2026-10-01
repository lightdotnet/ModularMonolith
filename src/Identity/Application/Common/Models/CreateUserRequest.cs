using StarterKit.Modules.Identity.Domain;
using System.ComponentModel.DataAnnotations;

namespace StarterKit.Modules.Identity.Application.Common.Models;

public record CreateUserRequest
{
    public string UserName { get; set; } = null!;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Password { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? AuthProvider { get; set; }
}
