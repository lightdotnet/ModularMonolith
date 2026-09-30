import { KeyRound } from "lucide-react";
import { ROLES_PERMISSIONS } from "./permissions";
import type { NavItem } from "@/types/nav";

export const ROLES_NAV_ITEM: NavItem = {
  label: "Roles",
  href: "/identity/roles",
  icon: KeyRound,
  permission: ROLES_PERMISSIONS.View,
};
