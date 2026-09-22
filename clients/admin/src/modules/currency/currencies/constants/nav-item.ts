import { Coins } from "lucide-react";
import { CURRENCY_PERMISSIONS } from "@/modules/currency/common/constants/permissions";
import type { NavItem } from "@/types/nav";

export const CURRENCIES_NAV_ITEM: NavItem = {
  label: "Currencies",
  href: "/currency/currencies",
  icon: Coins,
  permission: CURRENCY_PERMISSIONS.Currencies.View,
};
