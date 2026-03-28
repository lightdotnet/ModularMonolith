namespace Monolith.Identity;

public class CreateRoleRequest
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }
}
