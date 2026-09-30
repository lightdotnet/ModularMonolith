namespace StarterKit.Modules.Identity.Models;

public record CreateRoleRequest
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }
}
