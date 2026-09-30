using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace StarterKit.Modules.Identity.Web.Admin;

/// <summary>
/// Removes the routes of disabled admin pages: the whole <c>/Admin</c> folder when
/// <see cref="IdentityAdminOptions.Enabled"/> is off, otherwise per sub-folder
/// (<c>Users</c>, <c>Roles</c>, <c>Permissions</c>). A page without selectors has no endpoint,
/// so a disabled page answers 404.
/// </summary>
internal sealed class AdminPagesConvention(IdentityAdminOptions options) : IPageRouteModelConvention
{
    public void Apply(PageRouteModel model)
    {
        var feature = AdminFeatures.FromPagePath(model.ViewEnginePath);

        if (feature is null)
            return;

        if (!options.IsEnabled(feature))
            model.Selectors.Clear();
    }
}
