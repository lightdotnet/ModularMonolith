using StarterKit.Locations.Api.Domain.LocationTypes;
using StarterKit.Shared.Entities;
using ValidationException = Light.Exceptions.ValidationException;

namespace StarterKit.Locations.Api.Domain.Locations;

/// <summary>
/// Aggregate root for a single physical-location node (Store/Warehouse/Terminal/Bin, or any other
/// data-driven <see cref="LocationTypes.LocationType"/>) in a self-referencing hierarchy. Every
/// state transition runs through a guarded behaviour method — <see cref="Create"/>,
/// <see cref="Update"/>, <see cref="Move"/> — rather than open setters. The allowed-parent-type
/// rule itself now lives on <see cref="LocationTypes.LocationType"/> (<c>AllowedParentTypeId</c> /
/// <c>CanHaveChildren</c>) rather than a hardcoded switch here — see <see cref="ValidateParent"/>.
/// The caller resolves both the location's own <see cref="LocationType"/> and (when relevant) the
/// parent's, and passes the resolved entities in; this aggregate never queries anything itself.
/// Cycle detection needs a database walk and is enforced by the caller (see
/// <c>MoveLocationCommandHandler</c>), not here — same split as Organization's
/// <c>MoveOrgUnitCommandHandler</c>.
/// </summary>
public class Location : AuditableEntity
{
    private Location()
    {
    }

    public string Name { get; private set; } = null!;

    public string Code { get; private set; } = null!;

    public string LocationTypeId { get; private set; } = null!;

    public string? ParentLocationId { get; private set; }

    public LocationStatus Status { get; private set; } = LocationStatus.Active;

    public virtual LocationType Type { get; private set; } = null!;

    public virtual Location? Parent { get; private set; }

    public virtual IList<Location> Children { get; private set; } = [];

    public static Location Create(
        string name,
        string code,
        LocationType type,
        string? parentLocationId,
        Location? parent)
    {
        ValidateParent(type, parent);

        return new Location
        {
            Name = name,
            Code = code,
            LocationTypeId = type.Id,
            Type = type,
            ParentLocationId = parentLocationId,
            Status = LocationStatus.Active,
        };
    }

    /// <summary>
    /// Edits the editable metadata fields. <see cref="Type"/> and the parent are immutable here —
    /// reparenting goes through <see cref="Move"/>, and changing <see cref="Type"/> after creation
    /// would invalidate the allowed-parent-type invariant against whatever parent/children already
    /// exist, so it is not supported.
    /// </summary>
    public void Update(
        string name,
        string code,
        LocationStatus status)
    {
        Name = name;
        Code = code;
        Status = status;
    }

    /// <summary>
    /// Reparents this location, re-validating the allowed-parent-type rule against the new parent.
    /// <paramref name="type"/> is this location's own resolved type (unchanged by the move, but
    /// required explicitly since this aggregate never assumes its own navigation properties are
    /// loaded). Self-parenting and cycle detection need a database walk and are the caller's
    /// responsibility (see <c>MoveLocationCommandHandler</c>) before this is invoked.
    /// </summary>
    public void Move(LocationType type, string? newParentLocationId, Location? newParent)
    {
        ValidateParent(type, newParent);

        ParentLocationId = newParentLocationId;
    }

    private static void ValidateParent(LocationType type, Location? parent)
    {
        if (type.AllowedParentTypeId is null)
        {
            if (parent is not null)
                throw Invalid(nameof(ParentLocationId), $"A {type.Name} location cannot have a parent.");
        }
        else
        {
            if (parent is null || parent.LocationTypeId != type.AllowedParentTypeId)
                throw Invalid(
                    nameof(ParentLocationId),
                    $"A {type.Name} location requires a parent of type {type.AllowedParentTypeId}.");
        }

        if (parent is not null && !parent.Type.CanHaveChildren)
            throw Invalid(nameof(ParentLocationId), $"A {parent.Type.Name} location cannot have child locations.");
    }

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
