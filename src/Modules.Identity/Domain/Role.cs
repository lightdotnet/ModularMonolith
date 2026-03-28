using Light.Domain.Entities.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Monolith.Identity.Domain;

public class Role : IdentityRole, IEntity<string>, IAuditable
{
    public Role() => Id = LightId.NewId();

    public string? Description { get; set; }

    public DateTimeOffset Created { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset? LastModified { get; set; }

    public string? LastModifiedBy { get; set; }

    public void Update(string? name, string? description)
    {
        Name = name;
        Description = description;
    }
}