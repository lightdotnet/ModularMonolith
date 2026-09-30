import type { CSSProperties } from "react";
import type { ExternalToast, ToasterProps } from "sonner";

export const TOAST_DURATION_MS = 4000;

const closeButtonClassName =
  "!absolute !left-auto !right-3 !top-1/2 ![transform:translateY(-50%)] !border-0 !bg-transparent !text-white/80 hover:!text-white";

const toastClassName =
  "!overflow-hidden !rounded-md !border-0 !shadow-lg !text-white !p-4 !gap-3 after:absolute after:inset-x-0 after:bottom-0 after:h-1 after:origin-left after:bg-white/60 after:content-[''] after:[animation-name:toast-progress] after:[animation-timing-function:linear] after:[animation-fill-mode:forwards] after:[animation-duration:var(--toast-progress-duration,4000ms)]";

/**
 * Saturated, toastr-style toast theme (solid accent colors, progress-bar
 * countdown, inline close button). Spread into any <Toaster toastOptions>
 * to reuse this look for a page-local toaster instance.
 */
export const saturatedToastOptions: ToasterProps["toastOptions"] = {
  unstyled: false,
  classNames: {
    toast: toastClassName,
    title: "!text-white !font-semibold !text-sm",
    description: "!text-white/90",
    icon: "!text-white",
    closeButton: closeButtonClassName,
    success: "!bg-emerald-500/75",
    error: "!bg-red-500/75",
    warning: "!bg-amber-500/75",
    info: "!bg-sky-500/75",
  },
};

export function getToastProgressStyle(
  durationMs: number = TOAST_DURATION_MS,
): CSSProperties {
  return { "--toast-progress-duration": `${durationMs}ms` } as CSSProperties;
}

/**
 * Per-call options (duration + progress-bar timing) to spread into any
 * toast.success/info/warning/error/toast() call using saturatedToastOptions.
 */
export function withToastProgress(
  durationMs: number = TOAST_DURATION_MS,
): ExternalToast {
  return {
    duration: durationMs,
    style: getToastProgressStyle(durationMs),
  };
}
