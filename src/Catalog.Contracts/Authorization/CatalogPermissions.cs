namespace StarterKit.Catalog.Contracts.Authorization;

public static class CatalogPermissions
{
    public const string Group = "catalog";

    public static class Categories
    {
        public const string View = $"{Group}.categories.view";

        public const string Manage = $"{Group}.categories.manage";
    }

    public static class Products
    {
        public const string View = $"{Group}.products.view";

        public const string Manage = $"{Group}.products.manage";
    }
}
