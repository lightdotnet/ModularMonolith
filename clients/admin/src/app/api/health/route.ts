/**
 * Liveness probe — deliberately trivial and unauthenticated. The deploy-recovery
 * flow (`lib/shared/deployment-recovery`) polls this from an errored tab to tell
 * "the server is back" from "still deploying" before it hard-reloads, and infra
 * health checks can use it too. `proxy.ts` already excludes `/api` from the auth
 * gate.
 */
export function GET() {
  return new Response(null, { status: 204 });
}

// Never prerender or cache — every hit must reach the running server.
export const dynamic = "force-dynamic";
