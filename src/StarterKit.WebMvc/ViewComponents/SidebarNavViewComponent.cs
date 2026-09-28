using Microsoft.AspNetCore.Mvc;
using StarterKit.WebMvc.Authorization;
using StarterKit.WebMvc.Web.Navigation;

namespace StarterKit.WebMvc.ViewComponents;

public sealed record SidebarNavItemViewModel(
    string Title,
    string Icon,
    string Url,
    bool IsActive);

public sealed record SidebarNavSectionViewModel(
    string? Title,
    IReadOnlyList<SidebarNavItemViewModel> Items);

/// <summary>
/// Renders <see cref="NavigationMenu"/>, keeping only the items the user may open and marking the
/// one matching the current path as active. Sections left empty are dropped.
/// </summary>
public sealed class SidebarNavViewComponent(
    IPermissionChecker permissionChecker)
    : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var path = $"{Request.PathBase}{Request.Path}".TrimEnd('/');
        var pathBase = Request.PathBase.Value ?? string.Empty;

        var sections = NavigationMenu.Sections
            .Select(section => new SidebarNavSectionViewModel(
                section.Title,
                section.Items
                    .Where(item => item.Permissions.Count == 0
                        || permissionChecker.HasAnyPermission(
                            HttpContext.User,
                            item.Permissions))
                    .Select(item => new SidebarNavItemViewModel(
                        item.Title,
                        item.Icon,
                        $"{pathBase}{item.Url}",
                        IsActive(
                            item,
                            path,
                            pathBase)))
                    .ToList()))
            .Where(section => section.Items.Count > 0)
            .ToList();

        return View(sections);
    }

    private static bool IsActive(
        NavItem item,
        string path,
        string pathBase)
    {
        var url = $"{pathBase}{item.Url}".TrimEnd('/');

        if (item.ExactMatch
            ? string.Equals(path, url, StringComparison.OrdinalIgnoreCase)
            : IsPrefix(path, url))
        {
            return true;
        }

        return item.ActivePrefixes.Any(prefix => IsPrefix(
            path,
            $"{pathBase}{prefix}".TrimEnd('/')));
    }

    private static bool IsPrefix(
        string path,
        string prefix)
    {
        return path.Equals(prefix, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith($"{prefix}/", StringComparison.OrdinalIgnoreCase);
    }
}
