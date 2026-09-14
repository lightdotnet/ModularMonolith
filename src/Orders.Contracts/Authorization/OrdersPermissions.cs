namespace StarterKit.Orders.Contracts.Authorization;

public static class OrdersPermissions
{
    public const string Group = "orders";

    public static class Orders
    {
        public const string View = $"{Group}.orders.view";

        public const string Manage = $"{Group}.orders.manage";
    }

    public static class Payments
    {
        public const string View = $"{Group}.payments.view";

        public const string Manage = $"{Group}.payments.manage";
    }
}
