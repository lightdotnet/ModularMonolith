"use client";

/**
 * Recovery for errors a deploy causes in a browser tab still running the
 * previous build. All of them are fixed by a fresh page load once the new
 * build is serving:
 *
 * - `UnrecognizedActionError` ("Failed to find Server Action") — the old tab
 *   calls a Server Action ID that no longer exists in the new build. Pinning
 *   `NEXT_SERVER_ACTIONS_ENCRYPTION_KEY` (see `.env.example`) makes this rare
 *   but an action whose own code changed between builds still hits it.
 * - "An unexpected response was received from the server." (Next error `E394`)
 *   — a Server Action fetch got HTML / a redirect instead of an action result,
 *   e.g. the request landed on the old build's route while the new build was
 *   coming up, or a proxy bounced the POST.
 * - `TypeError: Failed to fetch` / `fetch failed` — a Server Action or RSC
 *   navigation request made while the server was mid-restart during the deploy.
 * - `ChunkLoadError` / "Loading chunk … failed" / dynamic-import failures — the
 *   old HTML references hashed asset filenames the new build no longer has.
 *
 * Backend-API connectivity failures do NOT reach here: `guardCall` in
 * `lib/server/call-guard` already turns those into a `Result` the page renders
 * as a toast, so matching "Failed to fetch" here does not swallow them.
 *
 * Recovery reaches these errors while the tab's JS is still alive (a failed
 * Server Action / RSC fetch, a chunk load), so before hard-reloading it polls
 * `/api/health` and only reloads once the server answers — otherwise a reload
 * fired mid-deploy lands on the browser's own "can't reach this page" screen,
 * where no app code runs and the recovery loop is dead. Nothing here can help a
 * tab that is *already* on that browser screen.
 */

const STATE_KEY = "admin:deployment-recovery";
const HEALTH_PATH = "/api/health";
const PROBE_TIMEOUT_MS = 5_000;
// A deploy can take tens of seconds — probe on a growing backoff (~67s total)
// instead of burning every attempt in the first ~6s.
const PROBE_DELAYS_MS = [2_000, 5_000, 10_000, 20_000, 30_000] as const;
// Cap on actual hard-reloads (tracked across reloads in sessionStorage) so a
// non-transient error that survives a reload can't loop forever.
const MAX_RELOADS = PROBE_DELAYS_MS.length;
// Must comfortably exceed the total backoff span so a mid-deploy attempt is not
// treated as "fresh" and allowed to loop past MAX_RELOADS.
const STATE_RESET_MS = 5 * 60_000;

type RecoveryState = { at: number; count: number };

/** True when `error` looks like a deploy-induced error a reload can clear. */
export function isRecoverableDeploymentError(error: unknown): boolean {
  if (!error || typeof error !== "object") return false;
  const { name, message } = error as { name?: unknown; message?: unknown };
  const code = (error as { __NEXT_ERROR_CODE?: unknown }).__NEXT_ERROR_CODE;

  if (
    name === "UnrecognizedActionError" ||
    name === "ChunkLoadError" ||
    code === "E394"
  ) {
    return true;
  }
  if (typeof message !== "string") return false;

  return (
    /failed-to-find-server-action|was not found on the server/i.test(message) ||
    // Server Action fetch got HTML / a redirect instead of an action result
    // (Next error E394) — matched by text too in case the code label changes.
    /an unexpected response was received from the server/i.test(message) ||
    // Network-layer fetch failure — server was mid-restart during the deploy.
    // Chrome: "Failed to fetch"; Firefox: "NetworkError when attempting to
    // fetch resource."; Safari: "Load failed".
    /failed to fetch|fetch failed|networkerror when attempting to fetch/i.test(
      message,
    ) ||
    message === "Load failed" ||
    // Old HTML pointing at asset filenames the new build no longer has.
    /loading (?:css )?chunk \S+ failed|error loading dynamically imported module|failed to fetch dynamically imported module|importing a module script failed/i.test(
      message,
    )
  );
}

function readState(): RecoveryState {
  try {
    const raw = window.sessionStorage.getItem(STATE_KEY);
    if (raw) {
      const parsed = JSON.parse(raw) as RecoveryState;
      if (
        typeof parsed?.at === "number" &&
        typeof parsed?.count === "number" &&
        Date.now() - parsed.at <= STATE_RESET_MS
      ) {
        return parsed;
      }
    }
  } catch {
    // ignore — treat as a fresh recovery below
  }
  return { at: 0, count: 0 };
}

/**
 * Whether {@link runDeploymentRecovery} would give up immediately — i.e.
 * {@link MAX_RELOADS} reloads have already happened in the last
 * {@link STATE_RESET_MS} without clearing the error. Pure read (no writes), safe
 * to call during render so the error UI can pick its copy before recovery runs.
 */
export function peekDeploymentRecoveryExhausted(): boolean {
  if (typeof window === "undefined") return false;
  return readState().count >= MAX_RELOADS;
}

function bumpReloadCount(state: RecoveryState): boolean {
  try {
    window.sessionStorage.setItem(
      STATE_KEY,
      JSON.stringify({ at: Date.now(), count: state.count + 1 }),
    );
    return true;
  } catch {
    return false;
  }
}

/** Resolves after `ms`, or early if `signal` aborts first. */
function sleep(ms: number, signal: AbortSignal): Promise<void> {
  return new Promise((resolve) => {
    if (signal.aborted) return resolve();
    const timer = window.setTimeout(resolve, ms);
    signal.addEventListener(
      "abort",
      () => {
        window.clearTimeout(timer);
        resolve();
      },
      { once: true },
    );
  });
}

/** True once `/api/health` answers — the new build is serving again. */
async function serverIsBack(outerSignal: AbortSignal): Promise<boolean> {
  const controller = new AbortController();
  const onAbort = () => controller.abort();
  outerSignal.addEventListener("abort", onAbort, { once: true });
  const timer = window.setTimeout(() => controller.abort(), PROBE_TIMEOUT_MS);
  try {
    const res = await fetch(HEALTH_PATH, {
      method: "GET",
      cache: "no-store",
      signal: controller.signal,
    });
    return res.ok;
  } catch {
    return false;
  } finally {
    window.clearTimeout(timer);
    outerSignal.removeEventListener("abort", onAbort);
  }
}

/**
 * Drives recovery from a deploy-induced error, from the still-interactive error
 * boundary: on a growing backoff ({@link PROBE_DELAYS_MS}) it probes
 * {@link HEALTH_PATH}, and only once the server answers does it hard-reload — so
 * the tab stays on the app's own "reconnecting" UI instead of being bounced to
 * the browser's native "can't reach this page" screen mid-deploy.
 *
 * `onExhausted` is called — without reloading — when {@link MAX_RELOADS} reloads
 * have already happened without clearing the error, or the probe never succeeds
 * within the backoff schedule. The returned function cancels the loop; call it
 * from the effect cleanup.
 */
export function runDeploymentRecovery(onExhausted: () => void): () => void {
  if (typeof window === "undefined") return () => {};

  const state = readState();
  if (state.count >= MAX_RELOADS) {
    onExhausted();
    return () => {};
  }

  const controller = new AbortController();

  void (async () => {
    for (const delay of PROBE_DELAYS_MS) {
      await sleep(delay, controller.signal);
      if (controller.signal.aborted) return;

      if (!(await serverIsBack(controller.signal))) continue;
      if (controller.signal.aborted) return;

      if (!bumpReloadCount(state)) {
        // Can't track reloads (sessionStorage blocked) — an untracked reload
        // risks a loop if the error is not actually deploy-transient. Bail to
        // the manual "Reload now" path instead.
        onExhausted();
        return;
      }
      window.location.reload();
      return;
    }
    if (!controller.signal.aborted) onExhausted();
  })();

  return () => controller.abort();
}
