import { ArrowLeftRight } from "lucide-react";
import { CURRENCY_PERMISSIONS } from "@/modules/currency/common/constants/permissions";
import type { NavItem } from "@/types/nav";

export const EXCHANGE_RATES_NAV_ITEM: NavItem = {
  label: "Exchange rates",
  href: "/currency/exchange-rates",
  icon: ArrowLeftRight,
  permission: CURRENCY_PERMISSIONS.Rates.View,
};
