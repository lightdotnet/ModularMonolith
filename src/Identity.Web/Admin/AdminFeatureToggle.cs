using Microsoft.Extensions.Options;
using StarterKit.Modules.Identity.Web.TagHelpers;

namespace StarterKit.Modules.Identity.Web.Admin;

/// <summary>
/// <see cref="IFeatureToggle"/> over <see cref="IdentityAdminOptions"/>.
/// </summary>
internal sealed class AdminFeatureToggle(IOptions<IdentityAdminOptions> options) : IFeatureToggle
{
    public bool IsEnabled(string feature) => options.Value.IsEnabled(feature);
}
