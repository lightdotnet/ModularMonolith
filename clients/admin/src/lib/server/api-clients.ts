import "server-only";

export const ApiClients = {
  Identity: "Identity",
  Notifications: "Notifications",
  Organization: "Organization",
  Approval: "Approval",
  LeaveManagement: "LeaveManagement",
  Location: "Location",
  Catalog: "Catalog",
  Orders: "Orders",
} as const;

export type ApiClientName = (typeof ApiClients)[keyof typeof ApiClients];
