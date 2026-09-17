using Light.Domain;
using Light.Domain.Entities.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace StarterKit.Identity.Api.Entities;

public class Role
    : IdentityRole, IEntity<string>, IHasAuditTime, IHasAuditUser
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