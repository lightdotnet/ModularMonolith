namespace StarterKit.Inventory.Contracts.Authorization;

public static class InventoryPermissions
{
    public const string Group = "inventory";

    public static class Stock
    {
        public const string View = $"{Group}.stock.view";

        public const string Manage = $"{Group}.stock.manage";
    }
}
