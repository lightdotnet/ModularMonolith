import { Users } from "lucide-react";
import { USERS_PERMISSIONS } from "./permissions";
import type { NavItem } from "@/types/nav";

export const USERS_NAV_ITEM: NavItem = {
  label: "Users",
  href: "/identity/users",
  icon: Users,
  permission: USERS_PERMISSIONS.View,
};
