"use client";

import { useEffect, useState } from "react";

function formatTime(ms: number): string {
  return new Date(ms).toLocaleTimeString();
}

function formatDate(ms: number): string {
  return new Date(ms).toLocaleString();
}

function formatCountdown(ms: number): string {
  const totalSeconds = Math.max(0, Math.floor(ms / 1000));
  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;

  if (hours > 0) return `${hours}h ${minutes}m`;
  if (minutes > 0) return `${minutes}m ${seconds}s`;
  return `${seconds}s`;
}

interface SessionLifecycleProps {
  expiresAt: number;
  sessionExpiresAt: number;
  refreshLeadMs: number;
}

export function SessionLifecycle({
  expiresAt,
  sessionExpiresAt,
  refreshLeadMs,
}: SessionLifecycleProps) {
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    const interval = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(interval);
  }, []);

  const nextRefreshAt = expiresAt - refreshLeadMs;
  const isAccessTokenStale = now >= expiresAt;
  const isRefreshingSoon = !isAccessTokenStale && now >= nextRefreshAt;
  const isSessionExpired = now >= sessionExpiresAt;

  return (
    <div className="grid gap-4 sm:grid-cols-3">
      <div className="rounded-2xl border border-border bg-muted/30 p-4">
        <p className="text-xs text-muted-foreground">Access token expires</p>
        <p className="mt-1 text-sm font-semibold text-foreground">
          {isAccessTokenStale ? "Refreshing" : formatTime(expiresAt)}
        </p>
        {!isAccessTokenStale && (
          <p className="text-xs text-muted-foreground">
            in {formatCountdown(expiresAt - now)}
          </p>
        )}
      </div>

      <div className="rounded-2xl border border-border bg-muted/30 p-4">
        <p className="text-xs text-muted-foreground">Next automatic refresh</p>
        <p className="mt-1 text-sm font-semibold text-foreground">
          {isRefreshingSoon || isAccessTokenStale
            ? "Refreshing shortly"
            : formatTime(nextRefreshAt)}
        </p>
      </div>

      <div className="rounded-2xl border border-border bg-muted/30 p-4">
        <p className="text-xs text-muted-foreground">Session expires</p>
        <p className="mt-1 text-sm font-semibold text-foreground">
          {formatDate(sessionExpiresAt)}
        </p>
        {isSessionExpired && (
          <p className="text-xs text-destructive">Session has expired.</p>
        )}
      </div>
    </div>
  );
}
