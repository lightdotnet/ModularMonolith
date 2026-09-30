"use client";

import { useSyncExternalStore } from "react";

const subscribe = () => () => {};

/** True only once the client has hydrated — avoids markup that depends on browser-only state (theme, viewport). */
export function useHasMounted(): boolean {
  return useSyncExternalStore(
    subscribe,
    () => true,
    () => false,
  );
}
