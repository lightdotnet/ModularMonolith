import { AppShell } from "@/components/layout/app-shell";
import { SessionGate } from "@/components/layout/session-gate";
import { resolveSession } from "@/modules/identity/user-profile";

export default async function DashboardLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  const session = await resolveSession();

  return (
    <SessionGate>
      <AppShell
        permissions={session?.permissions ?? []}
        userName={session?.profile?.userName ?? null}
        user={session?.profile ?? null}
      >
        {children}
      </AppShell>
    </SessionGate>
  );
}
