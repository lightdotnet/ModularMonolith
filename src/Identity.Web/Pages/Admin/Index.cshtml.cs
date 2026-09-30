using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Modules.Identity.Authorization;
using StarterKit.Modules.Identity.Web.Admin;
using StarterKit.Modules.Identity.Web.TagHelpers;

namespace StarterKit.Modules.Identity.Web.Pages.Admin;

[Authorize]
public class IndexModel(IFeatureToggle featureToggle) : AdminPageModel
{
    public IReadOnlyList<AdminSection> Sections { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        AdminSection[] candidates =
        [
            new(
                "Users",
                "Create accounts, edit profiles, assign roles and reset passwords.",
                "people",
                "/Admin/Users/Index",
                AdminFeatures.Users,
                IdentityPermissions.Users.View),
            new(
                "Roles",
                "Create roles and choose the permissions each role grants.",
                "person-badge",
                "/Admin/Roles/Index",
                AdminFeatures.Roles,
                IdentityPermissions.Roles.View),
            new(
                "Permissions",
                "Review and edit the role and permission matrix.",
                "shield-lock",
                "/Admin/Permissions/Index",
                AdminFeatures.Permissions,
                IdentityPermissions.Roles.View),
        ];

        var sections = new List<AdminSection>();

        foreach (var section in candidates)
        {
            if (featureToggle.IsEnabled(section.Feature)
                && await IsAuthorizedAsync(section.Permission))
            {
                sections.Add(section);
            }
        }

        if (sections.Count == 0)
            return Forbid();

        Sections = sections;
        return Page();
    }

    public sealed record AdminSection(
        string Title,
        string Description,
        string Icon,
        string PageName,
        string Feature,
        string Permission);
}
