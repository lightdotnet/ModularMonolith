namespace StarterKit.Inventory.Contracts.Authorization;

public static class InventoryPermissions
{
    public const string Group = "inventory";

    public static class Stock
    {
        public const string View = $"{Group}.stock.view";

        public const string Manage = $"{Group}.stock.manage";

        /// <summary>Reveals unit costs, stock values and valuation totals; without it every cost field is null.</summary>
        public const string ViewCost = $"{Group}.stock.view_cost";

        /// <summary>Allows setting a cost: a cost revaluation, or a manual inbound adjustment with an explicit unit cost.</summary>
        public const string Revalue = $"{Group}.stock.revalue";
    }
}
