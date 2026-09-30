interface DisplayableUser {
  userName: string;
  firstName?: string | null;
  lastName?: string | null;
}

export function getDisplayName(user: DisplayableUser): string {
  return user.firstName || user.lastName
    ? `${user.firstName ?? ""} ${user.lastName ?? ""}`.trim()
    : user.userName;
}

export function getInitials(user: DisplayableUser): string {
  const initials =
    `${user.firstName?.[0] ?? ""}${user.lastName?.[0] ?? ""}`.trim();
  return initials || user.userName.slice(0, 2).toUpperCase();
}
