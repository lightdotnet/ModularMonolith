namespace StarterKit.Identity.Contracts;

public class UserDto
{
    public string Id { get; set; } = null!;

    public string UserName { get; set; } = null!;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Status { get; set; }

    public string? AuthProvider { get; set; }

    public bool IsDeleted { get; set; }

    public IList<string> Roles { get; set; } = [];

    public IList<ClaimDto> Claims { get; set; } = [];
}