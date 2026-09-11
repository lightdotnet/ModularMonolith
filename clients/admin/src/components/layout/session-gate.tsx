"use client";

import { useEffect, useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import {
  ensureFreshSessionAction,
  type EnsureFreshSessionResult,
} from "@/modules/identity/auth/api/ensure-fresh-session-action";
import { SessionLoadingOverlay } from "@/components/layout/session-loading-overlay";
import { SessionUnreachableOverlay } from "@/components/layout/session-unreachable-overlay";

/** Backoff between retries for a transient (network/5xx) failure on the initial hard-navigation check, before giving up and showing a persistent "unreachable" state. */
const INITIAL_RETRY_DELAYS_MS = [1000, 2000];
/** Retry cadence once persistently unreachable — faster than the healthy keep-alive, since the user is actively waiting. */
const UNREACHABLE_RETRY_INTERVAL_MS = 5000;
/** How often to silently re-check/refresh in the background once healthy, while the app stays open without a hard reload. */
const KEEP_ALIVE_INTERVAL_MS = 60_000;

type GateStatus = "checking" | "ready" | "unreachable";

/**
 * Runs a session-freshness check on top of `children` (the rendered
 * `AppShell` tree). Shows `SessionLoadingOverlay` while the initial check is
 * in flight, and — if every retry keeps failing transiently (backend
 * unreachable, not a verdict on the token itself) — switches to a
 * persistent `SessionUnreachableOverlay` that keeps retrying instead of
 * silently falling back to (possibly stale) content. Mounts, and so runs
 * its initial check, only on a true hard navigation — nested layouts don't
 * remount on in-app soft navigation. See `ensure-fresh-session-action.ts`
 * for the server-side logic this drives.
 */
export function SessionGate({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const [status, setStatus] = useState<GateStatus>("checking");
  const [, startTransition] = useTransition();

  useEffect(() => {
    let cancelled = false;
    let retryTimeoutId: ReturnType<typeof setTimeout> | undefined;
    let pollIntervalId: ReturnType<typeof setInterval> | undefined;

    function becomeReady(result: EnsureFreshSessionResult) {
      if (cancelled) return;
      if (result.status === "updated") router.refresh();
      setStatus("ready");
      pollIntervalId = setInterval(runBackgroundCheck, KEEP_ALIVE_INTERVAL_MS);
    }

    function runInitialCheck(attempt: number) {
      startTransition(async () => {
        const result = await ensureFreshSessionAction({ refetchProfile: true });
        if (cancelled) return;

        if (result.status === "retry") {
          if (attempt < INITIAL_RETRY_DELAYS_MS.length) {
            retryTimeoutId = setTimeout(
              () => runInitialCheck(attempt + 1),
              INITIAL_RETRY_DELAYS_MS[attempt],
            );
            return;
          }
          setStatus("unreachable");
          pollIntervalId = setInterval(runUnreachableRetry, UNREACHABLE_RETRY_INTERVAL_MS);
          return;
        }

        becomeReady(result);
      });
    }

    function runUnreachableRetry() {
      startTransition(async () => {
        const result = await ensureFreshSessionAction({ refetchProfile: true });
        if (cancelled || result.status === "retry") return;
        if (pollIntervalId) clearInterval(pollIntervalId);
        becomeReady(result);
      });
    }

    function runBackgroundCheck() {
      startTransition(async () => {
        const result = await ensureFreshSessionAction({ refetchProfile: false });
        if (!cancelled && result.status === "updated") router.refresh();
      });
    }

    runInitialCheck(0);

    return () => {
      cancelled = true;
      if (retryTimeoutId) clearTimeout(retryTimeoutId);
      if (pollIntervalId) clearInterval(pollIntervalId);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- run once per hard-navigation mount only; router is a stable reference
  }, []);

  return (
    <>
      {children}
      {status === "checking" && <SessionLoadingOverlay />}
      {status === "unreachable" && <SessionUnreachableOverlay />}
    </>
  );
}
