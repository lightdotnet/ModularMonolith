/** Mirrors backend `LeaveManagementPermissions` — keep the string value in sync with the backend.
 * There is deliberately only one permission: viewing/creating/editing/deleting your own leave
 * requests needs no permission at all (every authenticated user can); `Manage` is the only real
 * gate, granting view-all + delete-any. */
export const LEAVE_REQUESTS_PERMISSIONS = {
  Manage: "leave.requests.manage",
} as const;
