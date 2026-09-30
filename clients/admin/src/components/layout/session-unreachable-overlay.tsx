import { Loader2Icon, WifiOffIcon } from "lucide-react";

/**
 * Full-page overlay `SessionGate` switches to once the initial hard-navigation
 * session check has exhausted its retries and is still failing transiently
 * (backend unreachable — not a verdict on the token itself). Stays up,
 * retrying in the background, until the backend answers again.
 */
export function SessionUnreachableOverlay() {
  return (
    <div className="fixed inset-0 z-overlay flex items-center justify-center bg-background/70 backdrop-blur-md">
      <div className="glass-panel flex flex-col items-center gap-4 px-10 py-8">
        <div className="flex size-14 items-center justify-center rounded-full bg-muted">
          <WifiOffIcon className="size-6 text-muted-foreground" />
        </div>
        <div className="flex flex-col items-center gap-1 text-center">
          <p className="text-sm font-medium text-foreground">Can&apos;t reach the server</p>
          <p className="flex items-center gap-1.5 text-xs text-muted-foreground">
            <Loader2Icon className="size-3.5 animate-spin" />
            Retrying…
          </p>
        </div>
      </div>
    </div>
  );
}
