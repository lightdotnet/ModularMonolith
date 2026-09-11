import { Button } from "@/components/ui/button";

/**
 * Plain top-level navigation (not a client-side click handler) into
 * `/login/microsoft/start`, which must run as a full page load since it
 * issues a same-origin redirect that itself gets redirected cross-origin to
 * Microsoft's OIDC endpoint.
 */
export function ExternalLoginLink({ returnTo }: { returnTo?: string }) {
  const href = returnTo
    ? `/login/microsoft/start?returnTo=${encodeURIComponent(returnTo)}`
    : "/login/microsoft/start";

  return (
    <Button asChild variant="outline" className="w-full">
      <a href={href}>Continue with Microsoft</a>
    </Button>
  );
}
