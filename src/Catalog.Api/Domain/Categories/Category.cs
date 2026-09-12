using StarterKit.Shared.Entities;

namespace StarterKit.Catalog.Api.Domain.Categories;

/// <summary>
/// Aggregate root for a single node in the self-referencing category tree. Simpler direct sibling of
/// <see cref="StarterKit.Locations.Api.Domain.Locations.Location"/> — no type discriminator, no
/// allowed-parent-type rule, so <see cref="Create"/>/<see cref="Rename"/>/<see cref="Move"/> carry no
/// real domain invariant of their own; they exist to keep every state transition behind a guarded
/// method rather than an open setter, matching this repo's aggregate style. Cycle detection needs a
/// database walk and is enforced by the caller (see <c>MoveCategoryCommandHandler</c>), not here —
/// same split as <c>MoveLocationCommandHandler</c>/<c>MoveOrgUnitCommandHandler</c>. Sibling-name
/// uniqueness under the same parent is enforced by a unique DB index plus a handler pre-check; see
/// <c>CreateCategoryCommandHandler</c> for the known root-level (<c>ParentCategoryId == null</c>)
/// gap.
/// </summary>
public class Category : AuditableEntity
{
    private Category()
    {
    }

    public string Name { get; private set; } = null!;

    public string? ParentCategoryId { get; private set; }

    public virtual Category? Parent { get; private set; }

    public virtual IList<Category> Children { get; private set; } = [];

    public static Category Create(
        string name,
        string? parentCategoryId)
    {
        return new Category
        {
            Name = name,
            ParentCategoryId = parentCategoryId,
        };
    }

    public void Rename(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Reparents this category. Self-parenting and cycle detection need a database walk and are the
    /// caller's responsibility (see <c>MoveCategoryCommandHandler</c>) before this is invoked.
    /// </summary>
    public void Move(string? newParentCategoryId)
    {
        ParentCategoryId = newParentCategoryId;
    }
}
