using StarterKit.Shared.Entities;

namespace StarterKit.Locations.Api.Domain.LocationTypes;

/// <summary>
/// Data-driven replacement for the old hardcoded <c>LocationType</c> enum — each row is a
/// location-type definition (e.g. Store/Warehouse/Terminal/Bin) that declares its own
/// allowed-parent-type rule via <see cref="AllowedParentTypeId"/> and <see cref="CanHaveChildren"/>.
/// <see cref="Locations.Location"/> reads these rules through its own
/// <c>ValidateParent</c> guard instead of a hardcoded switch. Unlike most aggregates in this
/// solution, <see cref="Id"/> is a caller-supplied business code (e.g. <c>"STORE"</c>), not a
/// framework-generated id — see <see cref="Create"/>.
/// </summary>
public class LocationType : AuditableEntity
{
    private LocationType()
    {
    }

    public string Name { get; private set; } = null!;

    /// <summary>
    /// The <see cref="LocationType"/> id that an instance of this type is allowed to have as a
    /// parent. <c>null</c> means instances of this type must be root locations — no parent allowed.
    /// </summary>
    public string? AllowedParentTypeId { get; private set; }

    public bool CanHaveChildren { get; private set; }

    public LocationTypeStatus Status { get; private set; } = LocationTypeStatus.Active;

    /// <summary>
    /// Creates a new location type with a caller-supplied <paramref name="id"/>. Whether that id is
    /// already taken, and whether <paramref name="allowedParentTypeId"/> references a real
    /// <see cref="LocationType"/>, need a database walk and are the caller's responsibility (see
    /// <c>CreateLocationTypeCommandHandler</c>). Field-shape validation (required/length) is
    /// FluentValidation's job on the incoming request, not this factory's — only real domain rules
    /// live here.
    /// </summary>
    public static LocationType Create(
        string id,
        string name,
        string? allowedParentTypeId,
        bool canHaveChildren)
    {
        return new LocationType
        {
            Id = id,
            Name = name,
            AllowedParentTypeId = allowedParentTypeId,
            CanHaveChildren = canHaveChildren,
            Status = LocationTypeStatus.Active,
        };
    }

    public void Update(
        string name,
        string? allowedParentTypeId,
        bool canHaveChildren,
        LocationTypeStatus status)
    {
        Name = name;
        AllowedParentTypeId = allowedParentTypeId;
        CanHaveChildren = canHaveChildren;
        Status = status;
    }
}
