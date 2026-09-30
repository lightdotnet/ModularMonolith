using Microsoft.AspNetCore.Mvc.ApplicationModels;
using StarterKit.Modules.Identity.Web.Admin;
using Xunit;

namespace Identity.Tests.Web.Admin;

public class AdminPagesConventionTests
{
    private static PageRouteModel CreateModel(string viewEnginePath)
    {
        var model = new PageRouteModel($"/Pages{viewEnginePath}.cshtml", viewEnginePath);
        model.Selectors.Add(new SelectorModel());
        return model;
    }

    [Theory]
    [InlineData("/Admin/Index")]
    [InlineData("/Admin/Users/Index")]
    [InlineData("/Admin/Roles/Edit")]
    [InlineData("/Admin/Permissions/Index")]
    public void Apply_WhenAdminDisabled_ShouldClearSelectorsOfEveryAdminPage(string path)
    {
        // Arrange
        var convention = new AdminPagesConvention(new IdentityAdminOptions { Enabled = false });
        var model = CreateModel(path);

        // Act
        convention.Apply(model);

        // Assert
        Assert.Empty(model.Selectors);
    }

    [Fact]
    public void Apply_WhenSectionDisabled_ShouldClearOnlyThatSection()
    {
        // Arrange
        var convention = new AdminPagesConvention(new IdentityAdminOptions { Users = false });
        var users = CreateModel("/Admin/Users/Edit");
        var roles = CreateModel("/Admin/Roles/Index");
        var overview = CreateModel("/Admin/Index");

        // Act
        convention.Apply(users);
        convention.Apply(roles);
        convention.Apply(overview);

        // Assert
        Assert.Empty(users.Selectors);
        Assert.Single(roles.Selectors);
        Assert.Single(overview.Selectors);
    }

    [Fact]
    public void Apply_WhenEverySectionDisabled_ShouldClearTheOverviewToo()
    {
        // Arrange
        var convention = new AdminPagesConvention(new IdentityAdminOptions
        {
            Users = false,
            Roles = false,
            Permissions = false,
        });
        var overview = CreateModel("/Admin/Index");

        // Act
        convention.Apply(overview);

        // Assert
        Assert.Empty(overview.Selectors);
    }

    [Theory]
    [InlineData("/Index")]
    [InlineData("/Account/Login")]
    [InlineData("/Administration/Index")]
    public void Apply_ShouldIgnoreNonAdminPages(string path)
    {
        // Arrange
        var convention = new AdminPagesConvention(new IdentityAdminOptions { Enabled = false });
        var model = CreateModel(path);

        // Act
        convention.Apply(model);

        // Assert
        Assert.Single(model.Selectors);
    }

    [Theory]
    [InlineData("/Admin/Index", AdminFeatures.Admin)]
    [InlineData("/Admin/Users/Index", AdminFeatures.Users)]
    [InlineData("/Admin/roles/Edit", AdminFeatures.Roles)]
    [InlineData("/Admin/Permissions/Index", AdminFeatures.Permissions)]
    [InlineData("/Account/Login", null)]
    public void FromPagePath_ShouldMapPageToFeature(string path, string? expected)
    {
        // Act
        var feature = AdminFeatures.FromPagePath(path);

        // Assert
        Assert.Equal(expected, feature);
    }
}
