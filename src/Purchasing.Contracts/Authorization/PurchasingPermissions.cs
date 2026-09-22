namespace StarterKit.Purchasing.Contracts.Authorization;

/// <summary>
/// There is deliberately no approve permission: a purchase order is decided by the approver chosen at
/// submission, through Approval's own step-approver check.
/// </summary>
public static class PurchasingPermissions
{
    public const string Group = "purchasing";

    public static class Suppliers
    {
        public const string View = $"{Group}.suppliers.view";

        public const string Manage = $"{Group}.suppliers.manage";
    }

    public static class Orders
    {
        public const string View = $"{Group}.orders.view";

        /// <summary>Create a draft purchase order, edit its header and lines, and cancel it.</summary>
        public const string Create = $"{Group}.orders.create";

        /// <summary>Submit for approval, resubmit after a rejection, and withdraw a pending submission.</summary>
        public const string Submit = $"{Group}.orders.submit";

        public const string Close = $"{Group}.orders.close";
    }

    public static class Receipts
    {
        public const string View = $"{Group}.receipts.view";

        public const string Create = $"{Group}.receipts.create";
    }

    public static class Returns
    {
        public const string View = $"{Group}.returns.view";

        /// <summary>Create, edit, post, cancel and credit purchase returns.</summary>
        public const string Create = $"{Group}.returns.create";

        /// <summary>Record the credit note the supplier issued against a posted return.</summary>
        public const string Credit = $"{Group}.returns.credit";
    }
}
