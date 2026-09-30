import { Settings, ShieldCog } from "lucide-react";
// Imported directly (not via each feature's barrel): the barrels also re-export
// server-only code (Server Components, session-backed API calls), and this file
// is reachable from the client-side Sidebar — pulling in the full barrel would
// drag that server-only chain into the client bundle.
import { HOME_NAV_ITEM } from "@/features/home/constants/nav-item";
import { USERS_NAV_ITEM } from "@/modules/identity/users/constants/nav-item";
import { ROLES_NAV_ITEM } from "@/modules/identity/roles/constants/nav-item";
import { NOTIFICATIONS_NAV_ITEM } from "@/modules/notifications/constants/nav-item";
import type { NavItem } from "@/types/nav";

export const NAV_ITEMS: NavItem[] = [
  HOME_NAV_ITEM,
  {
    label: "Administration",
    href: "/administration",
    icon: ShieldCog,
    children: [
      USERS_NAV_ITEM,
      ROLES_NAV_ITEM,
      NOTIFICATIONS_NAV_ITEM,
    ],
  },
  {
    label: "Settings",
    href: "/settings",
    icon: Settings,
  },
];
