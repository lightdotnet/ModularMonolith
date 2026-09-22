/** Mirrors Transfers.Contracts/Authorization/TransfersPermissions.cs (Transfers.*). */
export const TRANSFERS_PERMISSIONS = {
  View: "transfers.transfers.view",
  /** Create a draft, edit its header and lines, and cancel it while still a draft. */
  Create: "transfers.transfers.create",
  Dispatch: "transfers.transfers.dispatch",
  Receive: "transfers.transfers.receive",
  Close: "transfers.transfers.close",
} as const;
