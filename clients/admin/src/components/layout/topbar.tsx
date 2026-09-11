"use client";

import { Menu, PanelLeftClose, PanelLeftOpen } from "lucide-react";
import { cn } from "@/lib/shared/utils";
import { useScrolled } from "@/hooks/use-scrolled";
import { useSidebar } from "@/hooks/use-sidebar";
import { Button } from "@/components/ui/button";
import { Breadcrumbs } from "@/components/layout/breadcrumbs";
import { Brand } from "@/components/layout/brand";
import { SearchBox } from "@/components/shared/search-box";
import { ThemeToggle, AccentColorPicker } from "@/components/theme";
import { UserMenu } from "@/components/layout/user-menu";
import { NotificationBell } from "@/modules/notifications/components/notification-bell";
import type { ProfileData } from "@/types/session";

export function TopBar({
  user,
  permissions,
  userName,
}: {
  user: ProfileData | null;
  permissions: string[];
  userName: string | null;
}) {
  const scrolled = useScrolled();
  const { hidden, toggleSidebar, setMobileOpen } = useSidebar();

  return (
    <header
      className={cn(
        "sticky top-0 z-topbar flex h-(--topbar-height) w-full shrink-0 items-center gap-3 border-b bg-sidebar px-4 text-sidebar-foreground transition-all duration-fast ease-standard",
        scrolled
          ? "border-sidebar-border bg-sidebar/70 shadow-soft-sm backdrop-blur-md"
          : "border-transparent",
      )}
    >
      {/* Mobile — opens the overlay drawer. */}
      <Button
        variant="ghost"
        size="icon"
        className="lg:hidden"
        onClick={() => setMobileOpen(true)}
        aria-label="Open navigation"
      >
        <Menu />
      </Button>

      {/* Desktop — shows/hides the sidebar entirely. */}
      <Button
        variant="ghost"
        size="icon"
        className="hidden lg:inline-flex"
        onClick={toggleSidebar}
        aria-label={hidden ? "Show sidebar" : "Hide sidebar"}
      >
        {hidden ? <PanelLeftOpen /> : <PanelLeftClose />}
      </Button>

      <Brand />

      <div className="max-lg:hidden">
        <Breadcrumbs />
      </div>

      <div className="ml-auto flex items-center gap-2">
        <SearchBox permissions={permissions} userName={userName} />
        <AccentColorPicker />
        <ThemeToggle />

        <NotificationBell />

        <UserMenu user={user} />
      </div>
    </header>
  );
}
