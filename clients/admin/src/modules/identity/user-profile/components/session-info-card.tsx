import { RefreshCw } from "lucide-react";
import {
  Card,
  CardAction,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { REFRESH_LEAD_MS } from "@/lib/server/session-cookie";
import { refreshSessionAction } from "@/modules/identity/auth/api/refresh-session-action";
import { SessionLifecycle } from "@/modules/identity/user-profile/components/session-lifecycle";

export function SessionInfoCard({
  expiresAt,
  sessionExpiresAt,
  canManuallyRefresh = false,
}: {
  expiresAt: number;
  sessionExpiresAt: number;
  canManuallyRefresh?: boolean;
}) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Session</CardTitle>
        <CardDescription>Token lifecycle for the current sign-in.</CardDescription>
        {canManuallyRefresh && (
          <CardAction>
            <form action={refreshSessionAction}>
              <Button type="submit" variant="outline" size="sm">
                <RefreshCw />
                Refresh now
              </Button>
            </form>
          </CardAction>
        )}
      </CardHeader>
      <CardContent>
        <SessionLifecycle
          expiresAt={expiresAt}
          sessionExpiresAt={sessionExpiresAt}
          refreshLeadMs={REFRESH_LEAD_MS}
        />
      </CardContent>
    </Card>
  );
}
