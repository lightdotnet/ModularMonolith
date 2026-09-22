/** Mirrors Purchasing.Contracts/Authorization/PurchasingPermissions.cs (purchasing.*). */
export const PURCHASING_PERMISSIONS = {
  Suppliers: {
    View: "purchasing.suppliers.view",
    Manage: "purchasing.suppliers.manage",
  },
  Orders: {
    View: "purchasing.orders.view",
    /** Create a draft, edit its header and lines, and cancel it while still a draft or rejected. */
    Create: "purchasing.orders.create",
    /** Submit, resubmit, withdraw, and look up approver candidates. */
    Submit: "purchasing.orders.submit",
    /** Close a partially received order; also cancel an Approved order and edit/cancel other users' drafts. */
    Close: "purchasing.orders.close",
  },
  Receipts: {
    View: "purchasing.receipts.view",
    Create: "purchasing.receipts.create",
  },
  Returns: {
    View: "purchasing.returns.view",
    /** Create/edit a draft, post it, or cancel it. */
    Create: "purchasing.returns.create",
    Credit: "purchasing.returns.credit",
  },
} as const;
