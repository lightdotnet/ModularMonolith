using Light.AspNetCore.Authorization;

namespace StarterKit.Catalog.Contracts.Authorization;

public class CatalogPermissionProvider : IPermissionDefinitionProvider
{
    public IEnumerable<PermissionDefinition> Define()
    {
        yield return new(
            CatalogPermissions.Categories.View,
            "View Categories",
            CatalogPermissions.Group);

        yield return new(
            CatalogPermissions.Categories.Manage,
            "Manage Categories",
            CatalogPermissions.Group);

        yield return new(
            CatalogPermissions.Products.View,
            "View Products",
            CatalogPermissions.Group);

        yield return new(
            CatalogPermissions.Products.Manage,
            "Manage Products",
            CatalogPermissions.Group);
    }
}
