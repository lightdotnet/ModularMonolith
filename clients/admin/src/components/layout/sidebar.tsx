"use client";

import { useMemo } from "react";
import { cn } from "@/lib/shared/utils";
import { useSidebar } from "@/hooks/use-sidebar";
import { SidebarNavItem } from "@/components/layout/sidebar-nav-item";
import { Sheet, SheetContent, SheetTitle } from "@/components/ui/sheet";
import { NAV_ITEMS } from "@/constants/nav-items";
import { buildVisibleMenu } from "@/lib/shared/menu";
import { hasPermission } from "@/lib/shared/authorization";
import type { NavItem } from "@/types/nav";

function SidebarNav({ menu }: { menu: NavItem[] }) {
  return (
    <nav className="flex-1 space-y-0.5 overflow-y-auto px-2.5 py-2">
      {menu.map((item) => (
        <SidebarNavItem key={item.href} item={item} />
      ))}
    </nav>
  );
}

export function Sidebar({
  permissions,
  userName,
}: {
  permissions: string[];
  userName: string | null;
}) {
  const { hidden, mobileOpen, setMobileOpen } = useSidebar();

  const menu = useMemo(() => {
    const can = (permission: string) => hasPermission(permissions, userName, permission);
    return buildVisibleMenu(NAV_ITEMS, can);
  }, [permissions, userName]);

  return (
    <>
      {/* Desktop — hangs below the TopBar, level with the main content; fully
          hides (width 0) rather than collapsing to an icon rail. */}
      <aside
        inert={hidden}
        className={cn(
          "sticky top-(--topbar-height) z-sidebar hidden h-[calc(100dvh-var(--topbar-height))] shrink-0 flex-col overflow-hidden bg-sidebar text-sidebar-foreground transition-[width] duration-base ease-standard lg:flex",
          hidden ? "w-0" : "w-(--sidebar-width)",
        )}
      >
        <SidebarNav menu={menu} />
      </aside>

      {/* Mobile — overlay drawer, always shows the full menu (never icon-only). */}
      <Sheet open={mobileOpen} onOpenChange={setMobileOpen}>
        <SheetContent side="left" className="w-(--sidebar-width) gap-0 p-0">
          <SheetTitle className="sr-only">Navigation</SheetTitle>
          <SidebarNav menu={menu} />
        </SheetContent>
      </Sheet>
    </>
  );
}
