export type UserStatusTone = "success" | "warning" | "danger" | "neutral";

const WARNING_STATUSES = new Set(["inactive", "locked", "suspended", "pending"]);
const DANGER_STATUSES = new Set(["deleted", "banned", "disabled"]);

export function getUserStatusTone(status?: string | null): UserStatusTone {
  const normalized = status?.toLowerCase();

  if (normalized === "active") return "success";
  if (normalized && WARNING_STATUSES.has(normalized)) return "warning";
  if (normalized && DANGER_STATUSES.has(normalized)) return "danger";
  return "neutral";
}
