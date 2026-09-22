import { MapPin } from "lucide-react";
import { LOCATIONS_PERMISSIONS } from "./permissions";
import type { NavItem } from "@/types/nav";

export const LOCATION_NAV_ITEM: NavItem = {
  label: "Locations",
  href: "/location",
  icon: MapPin,
  permission: LOCATIONS_PERMISSIONS.View,
};
