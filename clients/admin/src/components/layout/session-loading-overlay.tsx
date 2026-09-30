import { Loader2Icon } from "lucide-react";

/**
 * Full-page overlay shown by `SessionGate` while a hard-navigation session
 * check is in flight — the token/profile may be stale or the refresh token
 * may have been revoked since the last visit, so nothing renders until this
 * resolves.
 */
export function SessionLoadingOverlay() {
  return (
    <div className="fixed inset-0 z-overlay flex items-center justify-center bg-background/70 backdrop-blur-md">
      <div className="glass-panel flex flex-col items-center gap-4 px-10 py-8">
        <div className="relative flex size-14 items-center justify-center">
          <span className="absolute inset-0 rounded-full bg-primary/15 animate-ping" />
          <span className="relative flex size-12 items-center justify-center rounded-full bg-primary text-primary-foreground shadow-soft-lg">
            <Loader2Icon className="size-6 animate-spin" />
          </span>
        </div>
        <p className="text-sm font-medium text-muted-foreground">Verifying your session…</p>
      </div>
    </div>
  );
}
