import { Loader2Icon, RefreshCwIcon } from "lucide-react";
import { Button } from "@/components/ui/button";

/**
 * Inner content for the deploy-recovery branch of the `error.tsx` boundaries.
 * Mirrors the `SessionLoadingOverlay` / `SessionUnreachableOverlay` visual
 * language.
 *
 * While a reload is still pending (`exhausted={false}`) it shows the animated
 * "connecting" treatment — an auto-reload is scheduled in the background. Once
 * the retries are spent (`exhausted={true}`) nothing runs in the background, so
 * the animation is dropped for a static prompt to reload manually.
 */
export function DeploymentRecoveryNotice({ exhausted }: { exhausted: boolean }) {
  return (
    <div className="flex flex-col items-center gap-4 text-center">
      <div className="relative flex size-14 items-center justify-center">
        {!exhausted && (
          <span className="absolute inset-0 animate-ping rounded-full bg-primary/15" />
        )}
        <span className="relative flex size-12 items-center justify-center rounded-full bg-primary text-primary-foreground shadow-soft-lg">
          {exhausted ? (
            <RefreshCwIcon className="size-6" />
          ) : (
            <Loader2Icon className="size-6 animate-spin" />
          )}
        </span>
      </div>

      <div className="flex flex-col items-center gap-1">
        <p className="text-sm font-medium text-foreground">
          {exhausted
            ? "The update is taking longer than expected."
            : "A new version is available"}
        </p>
        {!exhausted && (
          <p className="flex items-center gap-1.5 text-xs text-muted-foreground">
            <Loader2Icon className="size-3.5 animate-spin" />
            Reconnecting…
          </p>
        )}
      </div>

      <Button size="sm" variant="outline" onClick={() => window.location.reload()}>
        Reload now
      </Button>
    </div>
  );
}
