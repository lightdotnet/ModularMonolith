import { SidebarProvider } from "@/hooks/use-sidebar";
import { Sidebar } from "@/components/layout/sidebar";
import { TopBar } from "@/components/layout/topbar";
import { NotificationsProvider } from "@/modules/notifications/context/notifications-provider";
import type { ProfileData } from "@/types/session";

export function AppShell({
  permissions,
  userName,
  user,
  children,
}: {
  permissions: string[];
  userName: string | null;
  user: ProfileData | null;
  children: React.ReactNode;
}) {
  return (
    <SidebarProvider>
      <NotificationsProvider>
        <div className="flex min-h-full flex-col">
          <TopBar user={user} permissions={permissions} userName={userName} />
          <div className="flex flex-1">
            <Sidebar permissions={permissions} userName={userName} />
            <main className="min-w-0 flex-1 bg-sidebar p-4 sm:p-6">
              <div className="mx-auto max-w-7xl">{children}</div>
            </main>
          </div>
        </div>
      </NotificationsProvider>
    </SidebarProvider>
  );
}
